using DotPulsar;
using DotPulsar.Abstractions;
using DotPulsar.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace rPulsar.Pulsar;

/// <summary>
/// A class that consumes instances of a specific type from Apache Pulsar
/// </summary>
/// <typeparam name="T"></typeparam>
public class PulsarConsumer<T> : Consumer<T>, IAsyncDisposable, IHostedService
{
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly IConsumer<T> _consumer;
    private readonly ILogger<PulsarConsumer<T>>? _logger;
    private readonly TaskCompletionSource<bool> _seekSource = new();
    private readonly SubscriptionType _subscriptionType;

    private readonly TaskCompletionSource<bool> _stateSource = new();
    private bool _isSubscribed;
    private Task? _readTask;
    private Task? _monitorTask;


    public PulsarConsumer(
        ILogger<PulsarConsumer<T>>? logger,
        IPulsarClient client,
        IEnumerable<IForwardDestination<T>> destinations,
        string topic,
        string subscriptionName,
        SubscriptionType subscriptionType,
        ResultAggregator<bool>? resultAggregator = null
    )
        : base(
            destinations,
            topic,
            subscriptionName,
            resultAggregator
        )
    {
        _logger = logger;
        _subscriptionType = subscriptionType;
        _consumer = client.NewConsumer(JsonSchema<T>.Default)
            .SubscriptionName(subscriptionName)
            .SubscriptionType(GetPulsarSubscriptionType(subscriptionType))
            .Topic(topic)
            .StateChangedHandler(
                e =>
                {
                    switch (e.ConsumerState)
                    {
                        case DotPulsar.ConsumerState.Active:
                            State = ConsumerState.Active;
                            _isSubscribed = true;

                            if (!_stateSource.Task.IsCompleted)
                                _stateSource.SetResult(true);

                            break;

                        default:
                            State = ConsumerState.Inactive;
                            break;
                    }
                }
            )
            .Create();
    }

    private async Task Monitor(CancellationToken cancellationToken)
    {
        var state = DotPulsar.ConsumerState.Disconnected;
        while (!cancellationToken.IsCancellationRequested)
        {
            var newState = await _consumer.StateChangedFrom(
                state,
                cancellationToken
            );

            state = newState.ConsumerState;

            if (_logger == null)
                continue;

            switch (state)
            {
                case DotPulsar.ConsumerState.Active:
                    _logger.LogInformation(
                        "Consumer on topic {Topic} is now active.",
                        Topic
                    );
                    break;


                case DotPulsar.ConsumerState.Closed:
                    _logger.LogWarning(
                        "Consumer on topic {Topic} is now disconnected.",
                        Topic
                    );
                    break;


                case DotPulsar.ConsumerState.Disconnected:
                    _logger.LogWarning(
                        "Consumer on topic {Topic} is now disconnected.",
                        Topic
                    );
                    break;

                case DotPulsar.ConsumerState.Faulted:
                    _logger.LogError(
                        "Consumer on topic {Topic} is now faulted.",
                        Topic
                    );
                    break;

                case DotPulsar.ConsumerState.Inactive:
                    _logger.LogWarning(
                        "Consumer on topic {Topic} is now inactive.",
                        Topic
                    );
                    break;

                case DotPulsar.ConsumerState.ReachedEndOfTopic:
                    _logger.LogInformation(
                        "Consumer on topic {Topic} has reached the end of the topic.",
                        Topic
                    );
                    break;

                case DotPulsar.ConsumerState.Unsubscribed:
                    _logger.LogInformation(
                        "Consumer on topic {Topic} has been unsubscribed.",
                        Topic
                    );
                    break;
            }
        }
    }

    private static DotPulsar.SubscriptionType GetPulsarSubscriptionType(
        SubscriptionType subscriptionType
    ) =>
        subscriptionType switch
        {
            SubscriptionType.Exclusive => DotPulsar.SubscriptionType.Exclusive,
            SubscriptionType.Shared => DotPulsar.SubscriptionType.Shared,
            _ => throw new ArgumentException(
                "The given subscription type is not supported by this " +
                "consumer.",
                nameof(subscriptionType)
            )
        };

    protected override IEnumerable<Task> InitializationTasks =>
        new[]
        {
            _stateSource.Task,
            _seekSource.Task
        };

    public override async ValueTask DisposeAsync()
    {
        _cancellationTokenSource.Cancel();

        if (_isSubscribed)
        {
            _isSubscribed = false;
            await _consumer.Unsubscribe();
        }

        await _consumer.DisposeAsync();
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        _readTask = Task.Run(
            async () =>
            {
                try
                {
                    await Read(_consumer, _cancellationTokenSource.Token);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(
                        ex,
                        "An exception occurred while consuming messages."
                    );

                    throw;
                }
            },
            cancellationToken
        );

        _monitorTask = Task.Run(
            async () =>
            {
                try
                {
                    await Monitor(_cancellationTokenSource.Token);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(
                        ex,
                        "An exception occurred while monitoring the consumer."
                    );

                    throw;
                }
            },
            cancellationToken
        );

        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_readTask == null)
            return;

        if (_cancellationTokenSource.Token.CanBeCanceled)
        {
            var awaitRead = Cancel();
            _cancellationTokenSource.Cancel();

            if (awaitRead)
            {
                try
                {
                    await _readTask;
                }
                catch (TaskCanceledException)
                {
                }
            }
        }

        if (_isSubscribed)
        {
            _isSubscribed = false;

            // Shared subscriptions cant be unsubscribed
            if (_subscriptionType != SubscriptionType.Shared)
                await _consumer.Unsubscribe(cancellationToken);
        }
    }

    public override async Task CatchUp()
    {
        await _consumer.Seek(DateTime.Now);
    }

    private async Task Read(
        IConsumer<T> consumer,
        CancellationToken cancellationToken
    )
    {
        _logger?.LogInformation(
            "Initializing consumer on topic {0}",
            Topic
        );

        // await _consumer.Seek(MessageId.Latest, cancellationToken);
        _seekSource.SetResult(true);

        while (!cancellationToken.IsCancellationRequested)
        {
            var message = await consumer.Receive(cancellationToken);

            if (cancellationToken.IsCancellationRequested)
                return;

            var messageData = new MessageData();

            _logger?.LogInformation(
                "Received message from {0} (seq id {1}) on topic {2}",
                message.ProducerName,
                message.SequenceId,
                _consumer.Topic
            );

            var value = message.Value();
            var id = message.MessageId;

            _ = Task.Run(
                async () =>
                {
                    var success = await HandleReceive(
                        value,
                        messageData
                    );

                    if (success)
                        await consumer.Acknowledge(
                            id,
                            cancellationToken
                        );
                    else
                        await consumer
                            .RedeliverUnacknowledgedMessages(
                                [id],
                                cancellationToken
                            );
                },
                cancellationToken
            );
        }
    }
}
using DotPulsar.Abstractions;
using DotPulsar.Extensions;
using Microsoft.Extensions.Logging;

namespace rPulsar.Pulsar;

using DotPulsar;

/// <summary>
/// A producer that produces messages of a specific type for Pulsar
/// </summary>
/// <typeparam name="T">The type of message an instance of the class produces
/// </typeparam>
public class PulsarProducer<T> : Producer<T>, IAsyncDisposable
{
    private readonly ILogger<PulsarProducer<T>>? _logger;
    private readonly IProducer<T> _producer;

    public PulsarProducer(
        ILogger<PulsarProducer<T>>? logger,
        IPulsarClient client,
        string topic
    )
        : base(topic)
    {
        _logger = logger;
        _producer = client.NewProducer(JsonSchema<T>.Default)
            .Topic(Topic)
            .Create();

        // TODO: Make this run properly, perhaps as a hosted service
        Task.Run(() => Monitor(CancellationToken.None));
    }

    public async Task Monitor(CancellationToken cancellationToken)
    {
        var state = ProducerState.Disconnected;

        while (!cancellationToken.IsCancellationRequested)
        {
            state = await _producer.OnStateChangeFrom(state, TimeSpan.Zero, cancellationToken: cancellationToken);

            if (_logger == null)
                continue;

            switch (state)
            {
                case ProducerState.Closed:
                    _logger.LogWarning(
                        "Producer on topic {0} closed",
                        _producer.Topic
                    );
                    break;

                case ProducerState.Connected:
                    _logger.LogInformation(
                        "Producer on topic {0} connected",
                        _producer.Topic
                    );
                    break;

                case ProducerState.Disconnected:
                    _logger.LogWarning(
                        "Producer on topic {0} disconnected",
                        _producer.Topic
                    );

                    break;

                case ProducerState.Faulted:

                    _logger.LogError(
                        "Producer on topic {0} faulted",
                        _producer.Topic
                    );

                    break;

                case ProducerState.PartiallyConnected:
                    _logger.LogWarning(
                        "Producer on topic {0} partially connected",
                        _producer.Topic
                    );

                    break;
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _producer.DisposeAsync();
    }

    private void LogSend()
    {
        _logger?.LogInformation(
            "Sending message on topic {2}",
            _producer.Topic
        );
    }

    public override async ValueTask Send(T value)
    {
        LogSend();
        await _producer.Send(value);
    }

    public override async ValueTask Send(
        T value,
        CancellationToken cancellationToken
    )
    {
        LogSend();

        await _producer.Send(value, cancellationToken);
    }
}
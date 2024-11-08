using Microsoft.Extensions.Hosting;

namespace Common.Communication;

/// <summary>
/// A class that consumes instances of a specific type of class from a S2S
/// backend
/// </summary>
/// <typeparam name="T">The type of class the consumer should consume</typeparam>
public abstract class Consumer<T>(
    IEnumerable<IForwardDestination<T>> destinations,
    string topic,
    string subscriptionName,
    ResultAggregator<bool>? resultAggregator = null
)
    : IAsyncDisposable, IHostedService
{
    protected readonly ResultAggregator<bool> ResultAggregator = resultAggregator ?? ResultAggregators.All;
    private TaskCompletionSource<T> _consumeTaskSource = new();

    /// <summary>
    /// The S2S topic the consumer consumes from
    /// </summary>
    public string Topic { get; } = topic;

    /// <summary>
    /// The name of the subscription the consumer creates to the topic
    /// </summary>
    public string SubscriptionName { get; } = subscriptionName;

    protected IEnumerable<IForwardDestination<T>> Destinations { get; } = destinations;

    protected Task<T> ConsumeTask => _consumeTaskSource.Task;

    protected abstract IEnumerable<Task> InitializationTasks { get; }

    /// <summary>
    /// A task representing the initialization of the consumer. Once finished,
    /// the consumer has initialized
    /// </summary>
    public Task Initialization => Task.WhenAll(InitializationTasks);

    /// <summary>
    /// The current state of the consumer
    /// </summary>
    public ConsumerState State { get; protected set; } = ConsumerState.Inactive;


    public abstract ValueTask DisposeAsync();

    /// <summary>
    /// Asynchronously start the consumer, so that it can start consuming
    /// messages
    /// </summary>
    /// <param name="cancellationToken">A cancellation token used to potentially
    /// cancel the operation before it finishes.</param>
    /// <returns></returns>
    public abstract Task StartAsync(CancellationToken cancellationToken);
    
    /// <summary>
    /// Asynchronously stop the consumer, so that it no longer consumes messages
    /// </summary>
    /// <param name="cancellationToken">A cancellation token used to potentially
    /// cancel the operation before it finishes.</param>
    /// <returns></returns>
    public abstract Task StopAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Handles receiving a message by forwarding it to the predefined
    /// destinations
    /// </summary>
    /// <param name="message">The message to handle</param>
    /// <param name="data">Data regarding the message to handle</param>
    /// <returns>Whether or not the message should be marked as handled</returns>
    protected async Task<bool> HandleReceive(T message, MessageData data)
    {
        var result = await ResultAggregator.ApplyAsync(
            Destinations,
            new ParallelOptions(),
            async (destination, ct) => await destination.Handle(
                message,
                data,
                ct
            )
        );

        var src = _consumeTaskSource;
        _consumeTaskSource = new TaskCompletionSource<T>();
        src.SetResult(message);

        return result;
    }

    protected bool Cancel()
    {
        if (_consumeTaskSource.Task.IsCompleted)
            return false;
        
        _consumeTaskSource.SetCanceled();
        return true;
    }

    /// <summary>
    /// Forces the consumer to catch up to the current time, skipping
    /// previously sent messages not having been consumed already.
    /// </summary>
    /// <returns></returns>
    public abstract Task CatchUp();


    /// <summary>
    /// Await the next message consumes by the consumer
    /// </summary>
    /// <returns></returns>
    public async Task<T> Consume() =>
        await ConsumeTask;
}
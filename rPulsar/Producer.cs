namespace rPulsar;

/// <summary>
/// A class producing messages to be sent to a specific topic to the S2S broker
/// </summary>
/// <typeparam name="T">The type of message the class produces</typeparam>
public abstract class Producer<T>
{
    /// <summary>
    /// The topic the Producer produces messages for
    /// </summary>
    public string Topic { get; }

    protected Producer(string topic)
    {
        Topic = topic;
    }

    /// <summary>
    /// Asynchronously sends a message to the topic. The call is blocking,
    /// therefore, if it is awaited, it finished only when the message is
    /// successfully handled by a consumer.
    /// </summary>
    /// <param name="value">The message to send</param>
    /// <returns></returns>
    public abstract ValueTask Send(T value);

    /// <summary>
    /// Asynchronously sends a message to the topic. The call is blocking,
    /// therefore, if it is awaited, if finishes only when the message is
    /// successfully handled by a consumer. A cancellation token is provided
    /// that can potentially cancel the operation before it is finished. 
    /// </summary>
    /// <param name="value">The message to send</param>
    /// <param name="cancellationToken">A cancellation token used to potentially
    /// cancel the operation before it finishes</param>
    /// <returns></returns>
    public abstract ValueTask Send(
        T value,
        CancellationToken cancellationToken
    );
}
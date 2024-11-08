namespace Common.Communication;

/// <summary>
/// A destination of consumer messages of a specific type
/// </summary>
/// <typeparam name="TMessage">The type of message the destination handles
/// </typeparam>
public interface IForwardDestination<in TMessage>
{
    /// <summary>
    /// A function that handles receiving messages
    /// </summary>
    /// <param name="message">The message to handle</param>
    /// <param name="data">Data regarding the message to handle</param>
    /// <param name="cancellationToken">A cancellation token used to potentially
    /// cancel the operation before it finishes.</param>
    /// <returns>A boolean specifying whether the message should be considered
    /// to be handled or not.</returns>
    public ValueTask<bool> Handle(
        TMessage message,
        MessageData data,
        CancellationToken cancellationToken
    );
}
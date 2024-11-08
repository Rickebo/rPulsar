using rPulsar;

namespace Common.Communication;

/// <summary>
/// A mapping of a message destination handling a specific type of message to a
/// another message destination handling a super/parent type of message. 
/// </summary>
/// <typeparam name="TFrom">The type of message the mapping receives</typeparam>
/// <typeparam name="TTo">The type of message the mapping forwards the message
/// as</typeparam>
public class ForwardDestinationMap<TFrom, TTo>
    : IForwardDestination<TFrom>
    where TFrom : TTo
{
    private readonly IEnumerable<IForwardDestination<TTo>> _destinations;
    private readonly Func<IEnumerable<bool>, bool> _resultAggregator;

    /// <summary>
    /// Constructs an instance of the class using multiple destinations and a
    /// function to aggregate the result of the destinations.
    /// </summary>
    /// <param name="destinations">An enumerable containing the destinations
    /// to forward to</param>
    /// <param name="resultAggregator">A function responsible for aggregating
    /// the result of the destinations receiving a message into a single
    /// result</param>
    public ForwardDestinationMap(
        IEnumerable<IForwardDestination<TTo>> destinations,
        Func<IEnumerable<bool>, bool>? resultAggregator = null
    )
    {
        _destinations = destinations;
        _resultAggregator = resultAggregator ?? All;
    }

    /// <summary>
    /// An aggregator function requiring all destinations to evaluate to true
    /// for the mappings result to evaluate to true.
    /// </summary>
    public static Func<IEnumerable<bool>, bool> All =>
        booleans => booleans.All(b => b);

    /// <summary>
    /// An aggregator function requiring one of the destinations to evaluate to
    /// true for the mappings result to evaluate to true.
    /// </summary>
    public static Func<IEnumerable<bool>, bool> Any =>
        booleans => booleans.Any(b => b);

    /// <summary>
    /// Handles the message with the given data. Multiple destinations are
    /// handled in parallel. The result is the result of the aggregator function
    /// applied to the result of the destinations.
    /// </summary>
    /// <param name="message">The message to handle</param>
    /// <param name="data">The message data of the message to handle</param>
    /// <param name="cancellationToken">
    /// A cancellation token supporting
    /// cancelling the operation
    /// </param>
    /// <returns>
    /// The result of the aggregator function applied to the result of
    /// all the destination
    /// </returns>
    public async ValueTask<bool> Handle(
        TFrom message,
        MessageData data,
        CancellationToken cancellationToken
    )
    {
        return _resultAggregator(
            await AsyncExtensions.ForEachAsync(
                _destinations,
                new ParallelOptions
                {
                    CancellationToken = cancellationToken
                },
                async (destination, ct) =>
                    await destination.Handle(
                        message,
                        data,
                        ct
                    )
            )
        );
    }
}
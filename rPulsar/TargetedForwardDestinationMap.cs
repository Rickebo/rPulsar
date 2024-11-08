namespace Common.Communication;

/// <summary>
/// A class that forwards messages of type TFrom to destinations capable or
/// receiving messages of type TTo, where the destinations are of type
/// TTarget.
/// Useful to, for example, use with dependency injection to forward messages
/// of a specific type to a handler that handles a parent type of the message.
/// </summary>
/// <typeparam name="TFrom">
/// The type of message to forward.
/// Must be assignable to TTo.
/// </typeparam>
/// <typeparam name="TTo">
/// The type of message the destination accepts.
/// Must be assignable from TFrom..
/// </typeparam>
/// <typeparam name="TTarget">The type of the destination</typeparam>
public class TargetedForwardDestinationMap<TFrom, TTo, TTarget>
    : ForwardDestinationMap<TFrom, TTo>
    where TFrom : TTo
    where TTarget : IForwardDestination<TTo>
{
    public TargetedForwardDestinationMap(
        IEnumerable<TTarget> destinations
    )
        : base(
            destinations.Cast<IForwardDestination<TTo>>()
        )
    {
    }
}
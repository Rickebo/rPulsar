using System.Collections.Concurrent;

namespace rPulsar;

public class AsyncExtensions
{
    /// <summary>
    /// Run the given action on each task in parallel. The result is a
    /// an unordered enumerable of the results of the actions.
    /// </summary>
    /// <param name="source">The source data to process</param>
    /// <param name="options">
    /// The parallel options to pass to
    /// Parallel.ForEachAsync
    /// </param>
    /// <param name="action">
    /// The action to apply to instances of the
    /// source
    /// </param>
    /// <typeparam name="TInput">The type of instances in the source</typeparam>
    /// <typeparam name="TResult">
    /// The type of the result of the action,
    /// which is returned.
    /// </typeparam>
    /// <returns>An unsorted </returns>
    public static async ValueTask<IEnumerable<TResult>> ForEachAsync
        <TInput, TResult>(
            IEnumerable<TInput> source,
            ParallelOptions options,
            Func<TInput, CancellationToken, ValueTask<TResult>> action
        )
    {
        var results = new ConcurrentBag<TResult>();
        await Parallel.ForEachAsync(
            source,
            options,
            async (task, ct) =>
                results.Add(
                    await action(
                        task,
                        ct
                    )
                )
        );

        return results;
    }
}
using System.Collections.Concurrent;
using System.Linq.Expressions;
using rPulsar;

namespace rPulsar;

/// <summary>
/// A result aggregator class that aggregates the results of multiple functions
/// to one result 
/// </summary>
/// <typeparam name="T">The type of result to aggregate</typeparam>
public class ResultAggregator<T>
{
    private readonly Func<IEnumerable<T>, T> _aggregator;

    public ResultAggregator(
        Expression<Func<IEnumerable<T>, T>> expression
    )
    {
        _aggregator = expression.Compile();
    }

    public T Apply(IEnumerable<T> source)
    {
        return _aggregator(source);
    }

    /// <summary>
    /// Applies the function to multiple source values, using specified parallel
    /// options to produce one aggregated result
    /// </summary>
    /// <param name="source">The source values to apply the function to</param>
    /// <param name="options">Options specifying how the function is applied in
    /// parallel</param>
    /// <param name="selector">A function that converts the sources input types
    /// to the type the function aggregates</param>
    /// <typeparam name="TIn">The input type which the source contains</typeparam>
    /// <returns>The aggregated result</returns>
    public async ValueTask<T> ApplyAsync<TIn>(
        IEnumerable<TIn> source,
        ParallelOptions options,
        Func<TIn, CancellationToken, ValueTask<T>> selector
    )
    {
        return _aggregator(
            await AsyncExtensions.ForEachAsync(
                source,
                options,
                selector
            )
        );
    }

    
}
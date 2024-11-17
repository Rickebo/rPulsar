namespace rPulsar;

/// <summary>
/// Template result aggregators for booleans.
/// </summary>
public static class ResultAggregators
{
    /// <summary>
    /// Result aggregator requiring all source inputs to be true for the result
    /// to be true.
    /// </summary>
    public static ResultAggregator<bool> All =>
        new(
            source => source.All(b => b)
        );

    /// <summary>
    /// Result aggregator requiring any of the source inputs to be true for the
    /// the result to be true.
    /// </summary>
    public static ResultAggregator<bool> Any =>
        new(
            source => source.Any(b => b)
        );

    /// <summary>
    /// Result aggregator requiring none of the source inputs to be true for the
    /// result to be true
    /// </summary>
    public static ResultAggregator<bool> None =>
        new(
            source => source.All(b => !b)
        );

    /// <summary>
    /// Result aggregator requiring one of the source inputs to be false for the
    /// result to be true
    /// </summary>
    public static ResultAggregator<bool> NotOne =>
        new(
            source => source.Any(b => !b)
        );
}
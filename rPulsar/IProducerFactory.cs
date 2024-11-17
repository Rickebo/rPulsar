namespace rPulsar;

/// <summary>
/// An interface detailing how a ProducerFactory creates producers
/// </summary>
public interface IProducerFactory
{
    /// <summary>
    /// Creates a producer using a specified function to configure the
    /// <see cref="ProducerBuilder{T}"/> used to construct the producer
    /// </summary>
    /// <param name="configurator">The function that configures the
    /// <see cref="ProducerBuilder{T}"/></param>
    /// <typeparam name="T">The type of message the producer produces
    /// </typeparam>
    /// <returns>The resulting built instance of the
                     /// <see cref="Producer{T}"/></returns>
    public Producer<T> Create<T>(
        Func<
                ProducerBuilder<T>,
                ProducerBuilder<T>
            >
            configurator
    );
}
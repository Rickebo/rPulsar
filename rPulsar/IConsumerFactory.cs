namespace rPulsar;

/// <summary>
/// An interface of the ConsumerFactory to detail how consumers are constructed
/// </summary>
public interface IConsumerFactory
{
    public Consumer<T> Create<T>(
        Func<
                ConsumerBuilder<T>,
                ConsumerBuilder<T>
            >
            configurator
    );
}
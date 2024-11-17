namespace rPulsar;

/// <summary>
/// A builder class used to construct instances of the <see cref="Producer{T}"/>
/// class
/// </summary>
/// <typeparam name="T">The type of message the producer produces</typeparam>
public abstract class ProducerBuilder<T>
{
    protected string? Topic { get; private set; }

    public ProducerBuilder<T> WithTopic(string topic)
    {
        if (string.IsNullOrEmpty(topic))
            throw new InvalidOperationException("Topic is invalid.");
        
        Topic = topic;
        return this;
    }

    protected void Validate()
    {
        if (Topic == null)
            throw new InvalidOperationException("Topic is not set");
    }

    public abstract Producer<T> Build();
}
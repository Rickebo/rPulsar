namespace Common.Communication;

/// <summary>
/// An abstract builder class to construct instances of a Consumer class
/// </summary>
/// <typeparam name="T">The type of message the consumer consumes</typeparam>
public abstract class ConsumerBuilder<T>
{
    protected string? Topic { get; private set; }
    protected string? SubscriptionName { get; private set; }
    protected SubscriptionType SubscriptionType = SubscriptionType.Shared;

    /// <summary>
    /// Specifies the topic the consumer should subscribe to, and consume
    /// messages from
    /// </summary>
    /// <param name="topic">The name of the topic</param>
    /// <returns></returns>
    public ConsumerBuilder<T> WithTopic(string topic)
    {
        Topic = topic;
        return this;
    }

    /// <summary>
    /// Specifies the name of the subscription the consumer is to establish to
    /// its topic
    /// </summary>
    /// <param name="subscriptionName">The name of the subscription</param>
    /// <returns></returns>
    public ConsumerBuilder<T> WithSubscriptionName(string subscriptionName)
    {
        SubscriptionName = subscriptionName;
        return this;
    }
    
    /// <summary>
    /// Specifies the type of subscription the consumer is to establish to
    /// its topic
    /// </summary>
    /// <param name="subscriptionType">
    /// The type of subscription the consumer is to establish to its topic
    /// </param>
    /// <returns></returns>
    public ConsumerBuilder<T> WithSubscriptionType(SubscriptionType subscriptionType)
    {
        SubscriptionType = subscriptionType;
        return this;
    }

    /// <summary>
    /// Validates that the data contained in the builder is valid.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the data contained
    /// within the builder is invalid</exception>
    protected void Validate()
    {
        if (Topic == null)
            throw new InvalidOperationException("Topic is not set");

        if (SubscriptionName == null)
            throw new InvalidOperationException("Subscription name is not set");
    }

    public abstract Consumer<T> Build();
}
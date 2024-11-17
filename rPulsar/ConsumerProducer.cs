namespace rPulsar;

using System.Text.Json;

/// <summary>
/// A combined <see cref="Consumer"/> and <see cref="Producer"/> class to handle
/// consuming messages and producing them. Provides utilities to send messages
/// and consume a response to the messages sent. 
/// </summary>
/// <typeparam name="TRequest">The type of message the class produces</typeparam>
/// <typeparam name="TResponse">The type of message the class receives</typeparam>
public class ConsumerProducer<TRequest, TResponse>
{
    public Consumer<TResponse> Consumer { get; }
    public Producer<TRequest> Producer { get; }

    public ConsumerProducer(
        Producer<TRequest> producer,
        string responseTopic,
        IConsumerFactory consumerFactory
    )
    {
        Producer = producer;
        Consumer = consumerFactory.Create<TResponse>(
            builder => builder
                .WithTopic(responseTopic)
                .WithSubscriptionName("default")
        );
    }

    public ConsumerProducer(Consumer<TResponse> consumer, Producer<TRequest> producer)
    {
        Consumer = consumer;
        Producer = producer;
    }

    /// <summary>
    /// Asynchronously send a message using the Producer, and consume a response
    /// using the Consumer.
    /// </summary>
    /// <param name="message">The message to send</param>
    /// <returns>The received response</returns>
    public async Task<TResponse> SendReceive(TRequest message)
    {
        await Consumer.CatchUp();
        var receiveTask = Task.Run(() => Consumer.Consume());
        await Producer.Send(message);
        return await receiveTask;
    }

    /// <summary>
    /// Asynchronously send a message using the Producer, and receive a response
    /// of a specific type using the Consumer. 
    /// </summary>
    /// <param name="message">The message to send</param>
    /// <typeparam name="TResult">The type of response to receive</typeparam>
    /// <returns></returns>
    /// <exception cref="Exception">Thrown if the response could not be
    /// converted into the given type</exception>
    public async Task<TResult> SendReceiveAs<TResult>(TRequest message)
    {
        var obj = await SendReceive(message);

        return JsonSerializer.Deserialize<TResult>(
                JsonSerializer.Serialize(obj)
            ) ??
            throw new Exception(
                "Could not deserialize: resulting instance is null."
            );
    }
}
using DotPulsar.Abstractions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Common.Communication.Pulsar;

/// <summary>
/// A builder class to construct instances of the PulsarConsumer class   
/// </summary>
/// <typeparam name="T">The type of message the consumer consumes</typeparam>
public class PulsarConsumerBuilder<T>
    : ConsumerBuilder<T>
{
    private readonly IPulsarClient _client;
    private readonly IEnumerable<IForwardDestination<T>> _destinations;
    private readonly IServiceProvider _serviceProvider;

    public PulsarConsumerBuilder(
        IServiceProvider serviceProvider,
        IPulsarClient client,
        IEnumerable<IForwardDestination<T>> destinations
    )
    {
        _serviceProvider = serviceProvider;
        _client = client;
        _destinations = destinations;
    }

    public override PulsarConsumer<T> Build()
    {
        // Validate ensures Topic and SubscriptionName are set
        Validate();

        return new PulsarConsumer<T>(
            _serviceProvider.GetService<ILogger<PulsarConsumer<T>>>(),
            _client,
            _destinations,
            Topic!,
            SubscriptionName!,
            SubscriptionType
        );
    }
}
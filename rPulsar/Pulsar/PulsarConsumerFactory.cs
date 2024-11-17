using DotPulsar.Abstractions;

using Microsoft.Extensions.DependencyInjection;
using rPulsar;
using rPulsar.Pulsar;

namespace rPulsar.Pulsar;

/// <summary>
/// A factory class that produces consumers of the PulsarConsumer class using
/// the PulsarConsumerBuilder
/// </summary>
public class PulsarConsumerFactory : IConsumerFactory
{
    private readonly IPulsarClient _client;
    private readonly IServiceProvider _serviceProvider;

    public PulsarConsumerFactory(
        IPulsarClient client,
        IServiceProvider serviceProvider
    )
    {
        _client = client;
        _serviceProvider = serviceProvider;
    }

    public Consumer<T> Create<T>(
        Func<
                ConsumerBuilder<T>,
                ConsumerBuilder<T>
            >
            configurator
    ) =>
        configurator(
                new PulsarConsumerBuilder<T>(
                    _serviceProvider,
                    _client,
                    _serviceProvider.GetServices<IForwardDestination<T>>()
                )
            )
            .Build();
}
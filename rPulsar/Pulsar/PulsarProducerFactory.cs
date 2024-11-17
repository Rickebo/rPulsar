using DotPulsar.Abstractions;

namespace rPulsar.Pulsar;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// A factory class constructing instances of the
/// <see cref="PulsarProducer{T}"/> class using
/// the <see cref="PulsarProducerBuilder{T}"/>
/// </summary>
public class PulsarProducerFactory : IProducerFactory
{
    private readonly IPulsarClient _client;
    private readonly IServiceProvider _serviceProvider;

    public PulsarProducerFactory(
        IPulsarClient client,
        IServiceProvider serviceProvider
    )
    {
        _client = client;
        _serviceProvider = serviceProvider;
    }

    public Producer<T> Create<T>(
        Func<ProducerBuilder<T>, ProducerBuilder<T>> configurator
    ) =>
        configurator(
                new PulsarProducerBuilder<T>(
                    _serviceProvider,
                    _client
                )
            )
            .Build();
}
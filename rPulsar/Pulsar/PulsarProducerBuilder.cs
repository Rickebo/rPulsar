using DotPulsar.Abstractions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace rPulsar.Pulsar;

/// <summary>
/// A builder class to construct instances of the PulsarProducer class
/// </summary>
/// <typeparam name="T">The type of message the producer produces</typeparam>
public class PulsarProducerBuilder<T> : ProducerBuilder<T>
{
    private readonly IServiceProvider _serviceProvider;
    private IPulsarClient _client;

    public PulsarProducerBuilder(
        IServiceProvider serviceProvider,
        IPulsarClient client
    )
    {
        _serviceProvider = serviceProvider;
        _client = client;
    }

    public override PulsarProducer<T> Build()
    {
        // Validate ensures Topic is set
        Validate();

        return new PulsarProducer<T>(
            _serviceProvider.GetService<ILogger<PulsarProducer<T>>>(),
            _client,
            Topic!
        );
    }
}
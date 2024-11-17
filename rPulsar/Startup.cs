using DotPulsar;
using DotPulsar.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using rPulsar.Pulsar;

namespace rPulsar;

public static class Startup
{
    /// <summary>
    /// Adds a <see cref="Consumer{T}"/>, <see cref="Producer{T}"/> and
    /// <see cref="ConsumerProducer{TRequest,TResponse}"/> service to the
    /// service collection
    /// </summary>
    /// <param name="collection">
    /// The service collection to add the services to
    /// </param>
    /// <param name="producerConfigurator">
    /// A function that configures the ProducerBuilder
    /// </param>
    /// <param name="consumerConfigurator">
    /// A function that configures the ConsumerBuilder
    /// </param>
    /// <typeparam name="TRequest">
    /// The type of request the producer sends
    /// </typeparam>
    /// <typeparam name="TResponse">
    /// The type of request the consumer consumes
    /// </typeparam>
    /// <returns></returns>
    public static IServiceCollection AddConsumerProducer<TRequest, TResponse>(
        this IServiceCollection collection,
        Func<IServiceProvider, ProducerBuilder<TRequest>,
            ProducerBuilder<TRequest>> producerConfigurator,
        Func<IServiceProvider, ConsumerBuilder<TResponse>,
            ConsumerBuilder<TResponse>> consumerConfigurator
    ) =>
        collection
            .AddProducer(producerConfigurator)
            .AddConsumer(consumerConfigurator)
            .AddSingleton<ConsumerProducer<TRequest, TResponse>>();

    /// <summary>
    /// Adds Pulsar-related services to a specified service collection 
    /// </summary>
    /// <param name="collection">
    /// The service collection to add the services to
    /// </param>
    /// <returns></returns>
    public static IServiceCollection AddPulsar(this IServiceCollection collection) =>
        collection
            .AddSingleton<IPulsarClient>(
                provider =>
                {
                    var settings = provider.GetRequiredService<IPulsarSettings>();
                    if (!Uri.TryCreate(
                            settings.Url,
                            UriKind.Absolute,
                            out var parsedUri
                        ) || !parsedUri.Scheme.Equals(
                            "pulsar",
                            StringComparison.OrdinalIgnoreCase
                        ))
                        throw new Exception("Invalid pulsar URL provided in settings.");

                    var builder = PulsarClient.Builder();

                    if (settings.Url != null)
                        builder =
                            builder.ServiceUrl(new Uri(settings.Url));

                    return builder.Build();
                }
            )
            .AddTransient(
                typeof(PulsarProducerBuilder<>)
            )
            .AddTransient(
                typeof(PulsarConsumerBuilder<>)
            )
            .AddSingleton<IProducerFactory, PulsarProducerFactory>()
            .AddSingleton<IConsumerFactory, PulsarConsumerFactory>();

    /// <summary>
    /// Adds a <see cref="Producer{T}"/> to the specified service collection
    /// </summary>
    /// <param name="services">The service collection to add the producer to</param>
    /// <param name="configurator">A function that configures the
    /// <see cref="ProducerBuilder{T}"/> which is used to construct the
    /// producer.</param>
    /// <typeparam name="T">The type of message the producer produces</typeparam>
    /// <returns></returns>
    public static IServiceCollection AddProducer<T>(
        this IServiceCollection services,
        Func<IServiceProvider, ProducerBuilder<T>, ProducerBuilder<T>>
            configurator
    )
    {
        return services.AddSingleton<Producer<T>>(
            provider => provider
                .GetRequiredService<IProducerFactory>()
                .Create<T>(
                    builder => configurator(provider, builder)
                )
        );
    }

    /// <summary>
    /// Adds a <see cref="Consumer{T}"/> to the specified service collection
    /// </summary>
    /// <param name="services">The service collection to add the consuemr to
    /// </param>
    /// <param name="configurator">A function that configures the
    /// <see cref="ConsumerBuilder{T}"/> that is used to build the consumer
    /// </param>
    /// <typeparam name="T">The type of message the consumer consumes</typeparam>
    /// <returns></returns>
    public static IServiceCollection AddConsumer<T>(
        this IServiceCollection services,
        Func<IServiceProvider, ConsumerBuilder<T>, ConsumerBuilder<T>>
            configurator
    ) =>
        services
            .AddSingletonAndService(
                provider => provider
                    .GetRequiredService<IConsumerFactory>()
                    .Create<T>(
                        builder => configurator(provider, builder)
                    )
            );

    /// <summary>
    /// Adds a service to a service collection as a singleton and as a hosted
    /// service
    /// </summary>
    /// <param name="services">The service collection to add the service to
    /// </param>
    /// <param name="factory">A factory function that produces an instance of
    /// the service using a service provider</param>
    /// <typeparam name="T">The type of service to add to the service collection
    /// </typeparam>
    /// <returns></returns>
    private static IServiceCollection AddSingletonAndService<T>(
        this IServiceCollection services,
        Func<IServiceProvider, T>? factory = null
    ) where T : class, IHostedService
    {
        services = factory != null
            ? services.AddSingleton(factory)
            : services.AddSingleton<T>();

        return services.Any(
            service => service.ServiceType == typeof(HostedServiceContainer<T>)
        )
            ? services
            : services.AddHostedService<HostedServiceContainer<T>>();
    }

    /// <summary>
    /// Registers all <see cref="IForwardDestination{TMessage}"/> of a specified
    /// type
    /// </summary>
    /// <param name="services">The service collection to register the forwards
    /// to</param>
    /// <typeparam name="T">The type of class to register the forwards for
    /// </typeparam>
    /// <returns></returns>
    public static IServiceCollection RegisterForwards<T>(this IServiceCollection services)
    {
        var interfaces = typeof(T).GetInterfaces();

        foreach (var @interface in interfaces)
        {
            if (@interface.IsGenericType &&
                @interface.GetGenericTypeDefinition() ==
                typeof(IForwardDestination<>))
                services = services.AddSingleton(
                    @interface,
                    typeof(T)
                );
        }

        return services;
    }

    /// <summary>
    /// A helper class to manage multiple hosted services as one
    /// </summary>
    /// <typeparam name="T"></typeparam>
    private class HostedServiceContainer<T> : IHostedService
        where T : IHostedService
    {
        private readonly HashSet<T> _services = new();

        public HostedServiceContainer(IEnumerable<T> instances)
        {
            foreach (var instance in instances)
                _services.Add(instance);
        }

        public IEnumerable<T> GetServices() => _services;

        private Task ForAll(
            CancellationToken cancellationToken,
            Func<T, CancellationToken, Task> action
        ) =>
            Task.WhenAll(
                _services.Select(
                    service => action(service, cancellationToken)
                )
            );

        public Task StartAsync(CancellationToken cancellationToken) =>
            ForAll(
                cancellationToken,
                (service, token) => service.StartAsync(token)
            );

        public Task StopAsync(CancellationToken cancellationToken) =>
            ForAll(
                cancellationToken,
                (service, token) => service.StopAsync(token)
            );
    }
}
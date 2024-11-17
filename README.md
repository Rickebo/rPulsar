# rPulsar

Dependency injection library for Apache Pulsar in .NET 8

## Prerequisites

- [.NET 8.0](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) or later


## About

This library provides a simple way to use Apache Pulsar in .NET applications using 
dependency injection. It allows for easy configuration of producers and consumers
using the `AddPulsarProducer` and `AddPulsarConsumer` extension methods. The consumer can 
run asynchronously and trigger events when messages are received, or route messages to 
defined destinations.

## Example usage

### Setup:
```csharp
class PulsarSettings : IPulsarSettings {
    public string? Url { get; set; }
}

static class Startup {
    public static IServiceCollection AddPulsar(this IServiceCollection services) => 
        services
            .AddSingleton<IPulsarSettings>(new PulsarSettings {
                Url = "pulsar://localhost:6650"
            })
            .AddPulsar()
    }
}
```

### Injecting a producer and consumer: 

```csharp

class Message {
    string Content { get; set; }
}

var serviceCollection = new ServiceCollection();

serviceCollection
    .AddProducer<Message>(
        (provider, builder) => builder
            .WithTopic("example-topic")
    )
    .AddConsumer<Message>(
        (provider, builder) => builder
            .WithTopic(settings.PulsarTopic)
            .WithSubscriptionType(SubscriptionType.Exclusive)
            .WithSubscriptionName("example-subscription")
    )
```

### Producing messages:

```csharp
class Message {
    string Content { get; set; }
}

class MessageReader(Producer<Message> producer) {
    public async Task SendMessage(string content) {
        await producer.Send(new Message { Content = content });
    }
}
```

### Consuming messages:

```csharp
class Message {
    string Content { get; set; }
}

class MessageReader(Consumer<Message> consumer) {
    public async Task LogMessages(CancellationToken cancellationToken) {
        while (!cancellationToken.IsCancellationRequested) {
            var message = await consumer.Consume();
            Console.WriteLine($"Received message: {message.Content}");
        }
    }
}
```

### Routing messages:

The library supports registering routing rules for messages. This allows for forwarding 
messages to different destinations based on the message type.  


```csharp 
class Message {
    string Content { get; set; }
}

static class Startup {
    public static IServiceCollection AddRouting(this IServiceCollection services) => 
        services
            .AddConsumer<Message>(
                (_, builder) =>
                    builder
                        .WithTopic("example-topic")
                        .WithSubscriptionType(SubscriptionType.Shared)
            )
            .AddSingleton<MessageLogger>()
            .AddSingleton<IForwardDestination<Message>>(    
                provider => provider.GetRequiredService<MessageLogger>()
            );
}

class MessageLogger : IForwardDestination<Message> {
    public async ValueTask<bool> Forward(
        Message message,
        MessageData data,
        CancellationToken cancellationToken
    ) {
        Console.WriteLine($"Received message: {message.Content}");
        
        return true;
    }
}
```
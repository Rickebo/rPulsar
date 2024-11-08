using DotPulsar.Abstractions;

namespace Common.Communication.Pulsar;

public record PulsarClientWrapper(IPulsarClient Client);
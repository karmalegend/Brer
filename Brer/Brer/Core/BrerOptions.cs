using RabbitMQ.Client;

namespace Brer.Core;

public record BrerOptions(
    IConnectionFactory Factory,
    string ExchangeName,
    string QueueName);

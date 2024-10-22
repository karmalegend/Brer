using System;
using RabbitMQ.Client;

namespace Brer.Core;

public class BrerOptionsBuilder
{
    public const string DefaultLogin = "guest";
    public const int DefaultPort = 5672;
    public const string LocalHost = "localhost";
    private string Host { get; set; } = null!;
    private int Port { get; set; }

    private string ExchangeName { get; set; } = null!;
    private string QueueName { get; set; } = null!;

    private string RabbitMqUser { get; set; } = null!;
    private string RabbitMqPass { get; set; } = null!;
    private int? MaxRetries { get; set; }


    public BrerOptionsBuilder WithAddress(string host, int port)
    {
        Host = host;
        Port = port;
        return this;
    }

    public BrerOptionsBuilder WithExchange(string exchange)
    {
        ExchangeName = exchange;
        return this;
    }

    public BrerOptionsBuilder WithQueueName(string queue)
    {
        QueueName = queue;
        return this;
    }

    public BrerOptionsBuilder WithUsername(string username)
    {
        RabbitMqUser = username;
        return this;
    }

    public BrerOptionsBuilder WithPassword(string password)
    {
        RabbitMqPass = password;
        return this;
    }

    /// <summary>
    /// When this number is hit the message gets nacked Brer assumes you've properly setup a DLX.
    /// The message gets queued with x-first-death-reason set as rejected.
    /// </summary>
    /// <param name="maxRetries"></param>
    /// <returns></returns>
    public BrerOptionsBuilder WithMaxRetries(int maxRetries)
    {
        MaxRetries = maxRetries;
        return this;
    }

    public BrerOptionsBuilder ReadFromEnvironmentVariables()
    {
        Host = Environment.GetEnvironmentVariable("BrerHostName") ?? throw new ArgumentNullException(nameof(Host));
        Port = Convert.ToInt32(Environment.GetEnvironmentVariable("BrerPort") ??
                               throw new ArgumentNullException(nameof(Port)));
        ExchangeName = Environment.GetEnvironmentVariable("BrerExchangeName") ??
                       throw new ArgumentNullException(nameof(ExchangeName));
        QueueName = Environment.GetEnvironmentVariable("BrerQueueName") ??
                    throw new ArgumentNullException(nameof(QueueName));
        RabbitMqUser = Environment.GetEnvironmentVariable("BrerUserName") ??
                       throw new ArgumentNullException(nameof(RabbitMqUser));
        RabbitMqPass = Environment.GetEnvironmentVariable("BrerPassword") ??
                       throw new ArgumentNullException(nameof(RabbitMqPass));
        
        MaxRetries = int.TryParse(Environment.GetEnvironmentVariable("BrerMaxRetries"), out var parsedRetries)
            ? parsedRetries
            : null;
        
        return this;
    }

    public BrerOptions Build()
    {
        return new BrerOptions(new ConnectionFactory
            {
                Port = Port,
                HostName = Host,
                UserName = RabbitMqUser,
                Password = RabbitMqPass
            },
            ExchangeName,
            QueueName,
            MaxRetries);
    }
}

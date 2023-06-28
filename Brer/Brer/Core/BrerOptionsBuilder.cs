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

    public BrerOptionsBuilder WithUserName(string username)
    {
        RabbitMqUser = username;
        return this;
    }

    public BrerOptionsBuilder WithPassWord(string password)
    {
        RabbitMqPass = password;
        return this;
    }

    public BrerOptionsBuilder ReadFromEnviromentVariables()
    {
        Host = Environment.GetEnvironmentVariable("BrerHostName") ?? throw new ArgumentNullException(Host);
        Port = Convert.ToInt32(Environment.GetEnvironmentVariable("BrerPort") ??
                               throw new ArgumentNullException(nameof(Port)));
        ExchangeName = Environment.GetEnvironmentVariable("BrerExchangeName") ??
                       throw new ArgumentNullException(ExchangeName);
        QueueName = Environment.GetEnvironmentVariable("BrerQueueName") ?? throw new ArgumentNullException(QueueName);
        RabbitMqUser = Environment.GetEnvironmentVariable("BrerUserName") ??
                       throw new ArgumentNullException(RabbitMqUser);
        RabbitMqPass = Environment.GetEnvironmentVariable("BrerPassword") ??
                       throw new ArgumentNullException(RabbitMqPass);
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
            QueueName);
    }
}

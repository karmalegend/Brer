using System;
using Brer.Core.Interfaces;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Brer.Core;

internal class BrerContext : IBrerContext
{
    private readonly IConnection? _connection;

    public IConnection Connection =>
        _connection ?? throw new NullReferenceException("Failed to establish a connection");

    public BrerOptions BrerOptions { get; set; }
    public ILogger Logger { get; init; }
    private static readonly object LockObject = new();
    private bool _disposed;

    public BrerContext(BrerOptions options, ILogger<BrerContext> log)
    {
        Logger = log;
        BrerOptions = options;
        lock (LockObject)
        {
            if (_connection == null)
            {
                _connection = options.Factory.CreateConnection();
                using var channel = _connection.CreateModel();
                channel.ExchangeDeclare(options.ExchangeName, ExchangeType.Topic);
                Logger.LogInformation("Creating connection with Queue : {QueueName} on Exchange : {ExchangeName}",
                    BrerOptions.QueueName, BrerOptions.ExchangeName);
            }
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        if (disposing)
        {
            _connection?.Close();
            _connection?.Dispose();
        }

        _disposed = true;
    }
}

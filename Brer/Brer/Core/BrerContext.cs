using System;
using Brer.Core.Interfaces;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Brer.Core;

public class BrerContext : IBrerContext
{
    public IConnection? Connection { get; set; }
    public BrerOptions BrerOptions { get; set; }
    public ILogger Logger { get; init; }
    private static readonly object LockObject = new();
    private bool _disposed;

    public BrerContext(BrerOptions options, ILogger log)
    {
        Logger = log;
        BrerOptions = options;
        lock (LockObject)
        {
            if (Connection == null)
            {
                Connection = options.Factory.CreateConnection();
                using var channel = Connection.CreateModel();
                channel.ExchangeDeclare(options.ExchangeName, ExchangeType.Topic);
                Logger.LogInformation("Creating connection with {BrerOptions}", BrerOptions);
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
            Connection?.Dispose();
        }

        _disposed = true;
    }
}

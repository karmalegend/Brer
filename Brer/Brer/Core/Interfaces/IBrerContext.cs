using System;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Brer.Core.Interfaces;

public interface IBrerContext : IDisposable
{
    public IConnection Connection { get; }
    public BrerOptions BrerOptions { get; }
    public ILogger Logger { get; }
}

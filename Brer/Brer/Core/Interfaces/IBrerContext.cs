using System;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Brer.Core.Interfaces;

internal interface IBrerContext : IDisposable
{
    // All properties are internal by extension due to interface being internal
    public IConnection Connection { get; }
    public BrerOptions BrerOptions { get; }
    public ILogger Logger { get; }
}

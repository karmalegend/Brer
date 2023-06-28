using System.Threading;
using System.Threading.Tasks;
using Brer.Listener.Interfaces;
using Microsoft.Extensions.Hosting;

namespace Brer.Core
{
    public class BrerHostedService : IHostedService
    {
        private readonly IBrerListenerBuilder _builder;
        private IBrerListener? _listener;
 
        public BrerHostedService(IBrerListenerBuilder builder)
        {
            _builder = builder;
        }
 
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _listener = _builder.DiscoverAndSubscribeAll().Build();
            return Task.Run(() => _listener
                .StartListening()
                .StartReceivingEvents(),cancellationToken);
        }
 
        public Task StopAsync(CancellationToken cancellationToken)
        {
            _listener?.Dispose();
            return Task.CompletedTask;
        }
    }

}

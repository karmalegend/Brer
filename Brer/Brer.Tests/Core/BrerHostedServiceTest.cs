using Brer.Core;
using Brer.Listener.Interfaces;
using NSubstitute;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace BrerTests.Core;

public class BrerHostedServiceTest
{
  
    [Fact]
    public async Task Start_Async_Should_Start_Services_When_Called()
    {
        var mockBuilder = Substitute.For<IBrerListenerBuilder>();
        using var mockListener = Substitute.For<IBrerListener>();

        mockBuilder.DiscoverAndSubscribeAll().Returns(mockBuilder);
        mockBuilder.Build().Returns(mockListener);
        mockListener.StartListening().Returns(mockListener);

        var service = new BrerHostedService(mockBuilder);

        await service.StartAsync(new CancellationToken());

        mockBuilder.Received(1).DiscoverAndSubscribeAll();
        mockListener.Received(1).StartListening();
        mockListener.Received(1).StartReceivingEvents();
    }

    [Fact]
    public async Task Stop_Async_Should_Dispose_Listener_When_Stopped()
    {
        var mockBuilder = Substitute.For<IBrerListenerBuilder>();
        var mockListener = Substitute.For<IBrerListener>();
        
        mockBuilder.DiscoverAndSubscribeAll().Returns(mockBuilder);
        mockBuilder.Build().Returns(mockListener);
        mockListener.StartListening().Returns(mockListener);
    
    
        BrerHostedService service = new BrerHostedService(mockBuilder);
        await service.StartAsync(new CancellationToken());
    
        await service.StopAsync(new CancellationToken());
    
        mockListener.Received(1).Dispose();
    }
}

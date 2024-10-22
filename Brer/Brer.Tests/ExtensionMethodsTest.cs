using System.Linq;
using Brer;
using Brer.Core;
using Brer.Core.Interfaces;
using Brer.Listener;
using Brer.Publisher;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using Xunit;

namespace BrerTests;

public class ExtensionMethodsTest
{
    [Fact]
    public void UseBrer_ShouldRegisterTheApproriateServices_WhenCalled()
    {
        // Arrange
        IServiceCollection services = new ServiceCollection();

        var options = new BrerOptions(new ConnectionFactory(), "exchange", "queue",4);

        // Act
        services.UseBrer(options);

        // Assert
        services.Single(x => x.ServiceType == typeof(IBrerContext) && x.Lifetime == ServiceLifetime.Singleton).Should()
            .NotBeNull();
        
        services.Single(x => x.ImplementationType == typeof(BrerPublisher) && x.Lifetime == ServiceLifetime.Transient)
            .Should().NotBeNull();
        
        services.Single(x =>
                x.ImplementationType == typeof(BrerListenerBuilder) && x.Lifetime == ServiceLifetime.Transient).Should()
            .NotBeNull();
        
        services.Single(x =>
                x.ImplementationType == typeof(BrerHostedService) && x.ServiceType == typeof(IHostedService)).Should()
            .NotBeNull();

        services.Count.Should().Be(4);
    }
}

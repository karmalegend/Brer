using Brer.Core;
using BrerTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RabbitMQ.Client;
using Xunit;

namespace BrerTests.Core;

public class BrerContextTest
{
    [Fact]
    public void BrerContext_Should_Declare_An_Exchange_On_The_Specified_Channel_When_Initialized()
    {
        // Arrange
        var logger = Substitute.For<MockLogger<BrerContext>>();

        var connectionFactorySub = Substitute.For<IConnectionFactory>();
        var connectionSub = Substitute.For<IConnection>();
        var modelSub = Substitute.For<IModel>(); 

        connectionFactorySub.CreateConnection().Returns(connectionSub);

        connectionSub.CreateModel().Returns(modelSub);

        var options = new BrerOptions(connectionFactorySub, "Exchange", "Queue",null);

        // Act
        var res = new BrerContext(options, logger);

        // Assert
        logger.Received(1).Log(Arg.Is(LogLevel.Information),
            Arg.Is<string>("Creating connection with Queue : Queue on Exchange : Exchange"));
        
        connectionFactorySub.Received(1).CreateConnection();
        connectionSub.Received(1).CreateModel();
        modelSub.Received(1).ExchangeDeclare("Exchange",ExchangeType.Topic);

        res.BrerOptions.Should().BeEquivalentTo(options);
    }
}

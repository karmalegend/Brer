using System.Collections.Generic;
using Brer.Core;
using Brer.Core.Interfaces;
using Brer.Listener;
using Brer.Listener.Runtime.Interfaces;
using BrerTests.Helpers;
using FluentAssertions;
using NSubstitute;
using RabbitMQ.Client;
using Xunit;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace BrerTests.Listener;

public class BrerListenerTest
{
    [Fact]
    public void StartListening_ShouldRegisterTheCorrectTopics_WhenCalled()
    {
        // Arrange
        var context = Substitute.For<IBrerContext>();
        var connection = Substitute.For<IConnection>();
        var channel = Substitute.For<IModel>();
        var logger = Substitute.For<MockLogger<IBrerContext>>();
        

        context.Logger.Returns(logger);
        context.Connection.Returns(connection);
        context.BrerOptions.Returns(new BrerOptions(Substitute.For<IConnectionFactory>(), "MyExchange", "MyQueue"));
        connection.CreateModel().Returns(channel);

        var dispatcher = Substitute.For<IDispatcher>();

        var dispatchers = new Dictionary<string, IDispatcher>()
        {
            {"eventKey", dispatcher}
        };


        var sut = new BrerListener(context, dispatchers);

        // Act
        var res = sut.StartListening();

        // Assert
        _ = context.Received(1).Connection;
        channel.Received(1).ExchangeDeclare("MyExchange",ExchangeType.Topic);
        channel.Received(1).QueueDeclare("MyQueue", true, false, false);
        logger.Received(1).Log(LogLevel.Information,"Start Listening on queue MyQueue, exchange MyExchange, topic eventKey");
        channel.Received(1).QueueBind("MyQueue", "MyExchange", "eventKey");
        res.Should().BeOfType<BrerListener>();
    }
    
    // TODO: potentially look into spinning up an integration test for Receiving events.
}

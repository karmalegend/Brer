using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Brer.Core;
using Brer.Core.Interfaces;
using Brer.Listener;
using Brer.Listener.Runtime.Interfaces;
using BrerTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Xunit;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace BrerTests.Listener;

public class BrerListenerTest
{
    private readonly IBrerContext _context;
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly ILogger<IBrerContext> _logger;

    public BrerListenerTest()
    {
        _context = Substitute.For<IBrerContext>();
        _connection = Substitute.For<IConnection>();
        _channel = Substitute.For<IModel>();
        _logger = Substitute.For<MockLogger<IBrerContext>>();


        _context.Logger.Returns(_logger);
        _context.Connection.Returns(_connection);
        _context.BrerOptions.Returns(new BrerOptions(Substitute.For<IConnectionFactory>(), "MyExchange", "MyQueue"));
        _connection.CreateModel().Returns(_channel);
    }
    
    
    [Fact]
    public void StartListening_Should_Register_The_Correct_Topics_When_Called()
    {
        // Arrange
        var dispatcher = Substitute.For<IDispatcher>();

        var dispatchers = new Dictionary<string, IDispatcher>
        {
            {"eventKey", dispatcher}
        };

        var wildcardDispatchers = new Dictionary<string, IDispatcher>
        {
            {"eventKey.#", dispatcher},
            {"eventKey.*", dispatcher}
        };

        var sut = new BrerListener(_context, dispatchers, wildcardDispatchers);

        // Act
        var res = sut.StartListening();

        // Assert
        _ = _context.Received(1).Connection;
        _channel.Received(1).ExchangeDeclare("MyExchange", ExchangeType.Topic);
        _channel.Received(1).QueueDeclare("MyQueue", true, false, false);
        _logger.Received(1).Log(LogLevel.Information,
            "Start Listening on queue MyQueue, exchange MyExchange, topic eventKey");
        _channel.Received(1).QueueBind("MyQueue", "MyExchange", "eventKey");
        _channel.Received(1).QueueBind("MyQueue", "MyExchange", "eventKey.#");

        wildcardDispatchers.Keys.Should().Contain(@"eventKey\.[\w\.]+");
        wildcardDispatchers.Keys.Should().Contain(@"eventKey\.\w+");
        res.Should().BeOfType<BrerListener>();
    }

    [Theory]
    [InlineData("test.topic")]
    [InlineData("test.#")]
    public async Task EventReceived_Should_Requeue_With_Traces_And_Requeue_Count_When_Handler_Throws(string routingKey)
    {
        // Arrange
        var dispatcher = Substitute.For<IDispatcher>();
        var dispatchers = new Dictionary<string, IDispatcher> {{"test.topic", dispatcher}};
        var body = "test body"u8.ToArray();
        var eventArgs = new BasicDeliverEventArgs
        {
            RoutingKey = routingKey,
            Body = body,
            Exchange = "MyExchange",
            DeliveryTag = 1,
            BasicProperties = Substitute.For<IBasicProperties>()
        };

        var exception = new Exception("Test exception");
        // Configure the dispatcher to throw an exception when Dispatch is called
        dispatcher.Dispatch(Arg.Any<BasicDeliverEventArgs>())
            .Returns(Task.FromException(exception));


        var listener = new BrerListener(_context, dispatchers, new Dictionary<string, IDispatcher>());
        // set up the channel
        listener.StartListening();

        // Act
        InvokeProcessEvent(listener, dispatcher, eventArgs);

        // Assert

        // Verify that BasicPublish was called with the expected arguments
        _channel.Received(1).BasicPublish(
            "MyExchange", // Expected exchange name
            routingKey, // Expected routing key
            Arg.Any<bool>(),
            Arg.Is<IBasicProperties>(x =>
                (string) x.Headers["x-Brer-Exception-StackTrace"] == exception.StackTrace &&
                (string) x.Headers["x-Brer-Exception-Message"] == exception.Message &&
                (string) x.Headers["x-Brer-Exception"] == exception.GetType().ToString() &&
                Convert.ToInt32(x.Headers["x-Brer-RequeueCount"]) == 1),
            body
        );

        _channel.Received(1).BasicAck(eventArgs.DeliveryTag, false);
        _logger.Received(1).Log(LogLevel.Error, exception,
            $"Failed to handle event on exchange MyExchange with routing key {routingKey}. Consumer tag: (null)");
    }
    
    
    
    [Theory]
    [InlineData("test.topic")]
    [InlineData("test.#")]
    public async Task EventReceived_Should_Ack_When_Handler_Success(string routingKey)
    {
        // Arrange
        var dispatcher = Substitute.For<IDispatcher>();
        var dispatchers = new Dictionary<string, IDispatcher> {{"test.topic", dispatcher}};
        var body = "test body"u8.ToArray();
        var eventArgs = new BasicDeliverEventArgs
        {
            RoutingKey = routingKey,
            Body = body,
            Exchange = "MyExchange",
            DeliveryTag = 1,
            BasicProperties = Substitute.For<IBasicProperties>()
        };
        
        // Configure the dispatcher to throw an exception when Dispatch is called
        dispatcher.Dispatch(Arg.Any<BasicDeliverEventArgs>())
            .Returns(Task.CompletedTask);


        var listener = new BrerListener(_context, dispatchers, new Dictionary<string, IDispatcher>());
        // set up the channel
        listener.StartListening();

        // Act
        InvokeProcessEvent(listener, dispatcher, eventArgs);

        // Assert
        _channel.Received(0).BasicPublish(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(),
            Arg.Any<IBasicProperties>(), Arg.Any<ReadOnlyMemory<byte>>());

        _channel.Received(1).BasicAck(eventArgs.DeliveryTag, false);
        _logger.Received(1).Log(LogLevel.Information, $"Handled event on exchange MyExchange with routing key {routingKey}. Consumer tag: (null)");
    }

    private void InvokeProcessEvent(BrerListener listener, IDispatcher dispatcher, BasicDeliverEventArgs eventArgs)
    {
        // Use reflection to invoke the private method ProcessEvent
        var method = typeof(BrerListener).GetMethod("ProcessEvent",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method?.Invoke(listener, [dispatcher, eventArgs]);
    }
    

    // TODO: potentially look into spinning up an integration test for Receiving events.
}

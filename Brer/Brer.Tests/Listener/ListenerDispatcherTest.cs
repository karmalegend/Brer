using System;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Brer.Listener;
using BrerTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using NSubstitute;
using RabbitMQ.Client.Events;
using Xunit;

namespace BrerTests.Listener;

public class ListenerDispatcherTest : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IServiceScope _serviceScope;

    public ListenerDispatcherTest()
    {
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();
        _serviceScope = provider.CreateScope(); // creating the scope here

        _serviceProvider = Substitute.For<IServiceProvider>();

        // Mock the serviceProvider using the actual implementation
        _serviceProvider.GetService(Arg.Any<Type>())
            .Returns(args => _serviceScope.ServiceProvider.GetService(args.Arg<Type>()));
    }

    private static Type EventListenerType => typeof(ListenerDispatcherTestHandler);
    private static Type ParamType => typeof(ListenerDispatcherEventData);

    [Fact]
    public async Task Dispatch_Should_Invoke_EventListenerMethod_With_DeserializedParam()
    {
        // Arrange
        var invoker = Substitute.For<ListenerDispatcherTestHandler>();
        var e = new BasicDeliverEventArgs()
        {
            Body = Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(new ListenerDispatcherEventData
                {Data = "hello world"}))
        };

        _serviceProvider.GetService(EventListenerType).Returns(invoker);
        var sut = new ListenerDispatcher(EventListenerType, EventListenerType.GetMethod("Handle")!, ParamType,
            _serviceProvider);

        // Act
        var act = () => sut.Dispatch(e);

        // Assert
        // this is not how we want to test this
        // however given we dont strictly limit ourselves to eventlisteners registered in the DI container
        // it becomes quite a pain to test. This seems like a fair middle ground.
        (await act.Should().ThrowAsync<TargetInvocationException>()).WithInnerException<NotImplementedException>()
            .WithMessage("Handler reached with event-data: hello world");
    }

    [Fact]
    public async Task Dispatch_Should_Throw_If_Method_Throws()
    {
        // Arrange
        var invoker = Substitute.For<ListenerDispatcherTestHandler>();
        var e = new BasicDeliverEventArgs()
        {
            Body = Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(new ListenerDispatcherEventData
                {Data = "hello world"}))
        };

        _serviceProvider.GetService(EventListenerType).Returns(invoker);


        var sut = new ListenerDispatcher(EventListenerType, EventListenerType.GetMethod("HandleThatThrows")!, ParamType,
            _serviceProvider);

        // Act
        var act = () => sut.Dispatch(e);

        // Assert
        (await act.Should().ThrowAsync<TargetInvocationException>()).WithInnerException<InvalidCastException>()
            .WithMessage("ListenerDispatcherTestHandler.HandleThatThrows");
    }

    [Fact]
    public void Dispatch_Should_Throw_JsonSerializationException_If_Param_Deserialization_Fails()
    {
        // Arrange
        var e = new BasicDeliverEventArgs()
        {
            Body = Encoding.Unicode.GetBytes(JsonConvert.SerializeObject("Invalid Data"))
        };


        var sut = new ListenerDispatcher(EventListenerType, EventListenerType.GetMethod("HandleThatThrows")!, ParamType,
            _serviceProvider);

        // Act
        var act = () => sut.Dispatch(e);

        // Assert
        act.Should().ThrowAsync<JsonSerializationException>();
    }
    


    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        _serviceScope.Dispose();
    }
}

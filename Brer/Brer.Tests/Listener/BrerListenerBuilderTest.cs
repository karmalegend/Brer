using System;
using System.Linq;
using System.Reflection;
using Brer.Core.Interfaces;
using Brer.Listener;
using Brer.Listener.Runtime;
using BrerTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace BrerTests.Listener;

public class BrerListenerBuilderTest
{
    [Fact]
    public void Subscribe_ShouldRegisterDispatcher_WhenCalledWithAction()
    {
        // Arrange
        var topic = "MyTopic";

        var mockBrerContext = Substitute.For<IBrerContext>();
        var mockLogger = Substitute.For<MockLogger<IBrerContext>>();

        mockBrerContext.Logger.Returns(mockLogger);

        // we just need a random Action
        Action<BrerListenerBuilder> callback = _ => { };


        var sut = new BrerListenerBuilder(mockBrerContext, Substitute.For<IServiceProvider>());

        // Act
        sut.Subscribe(topic, callback);

        // Assert
        mockLogger.Received(1).Log(LogLevel.Information,
            "Subscribing MyTopic to System.Action`1[Brer.Listener.BrerListenerBuilder]");
        sut.Dispatchers.Keys.Should().Contain("MyTopic");
        sut.Dispatchers["MyTopic"].Should().BeOfType<CallBackDispatcher<BrerListenerBuilder>>();
    }

    [Fact]
    public void Subscribe_ShouldRegisterDispatcher_WhenCalledWithCallBackDispatcher()
    {
        // Arrange
        var topic = "MyTopic";

        var mockBrerContext = Substitute.For<IBrerContext>();
        var mockLogger = Substitute.For<MockLogger<IBrerContext>>();

        mockBrerContext.Logger.Returns(mockLogger);

        // we just need a random Action
        Action<BrerListenerBuilder> action = _ => { };
        var callback = new CallBackDispatcher<BrerListenerBuilder>(action);


        var sut = new BrerListenerBuilder(mockBrerContext, Substitute.For<IServiceProvider>());

        // Act
        sut.Subscribe(topic, callback);

        // Assert
        mockLogger.Received(1).Log(LogLevel.Information,
            "Subscribing MyTopic to Brer.Listener.Runtime.CallBackDispatcher`1[Brer.Listener.BrerListenerBuilder]");
        sut.Dispatchers.Keys.Should().Contain("MyTopic");
        sut.Dispatchers["MyTopic"].Should().BeOfType<CallBackDispatcher<BrerListenerBuilder>>();
    }

    [Fact]
    public void Subscribe_ShouldSubscribeTypeWithoutHandlers_WhenTypeHasEventListenerAttributeButNoHandlerAttributes()
    {
        // Arrange
        var mockBrerContext = Substitute.For<IBrerContext>();
        var mockLogger = Substitute.For<MockLogger<IBrerContext>>();
        var listenerWithoutHandlers = new BrerEventListenerWithoutHandlersMock().GetType();

        mockBrerContext.Logger.Returns(mockLogger);

        var sut = new BrerListenerBuilder(mockBrerContext, Substitute.For<IServiceProvider>());

        // Act
        sut.Subscribe(listenerWithoutHandlers);

        // Assert
        mockLogger.Received(1).Log(LogLevel.Information, "Subscribing BrerEventListenerWithoutHandlersMock");
        sut.Dispatchers.Keys.Count.Should().Be(0);
    }
    
    // this seems impossible to test as of now.
    // We've tried mocking TypeInfo but we're unable to register the required class attribute at runtime, as we don't
    // use the TypeDescriptor to retrieve the attributes. We've tried Mocking TypeInfo's GetAttributes but it isn't
    // abstract or virtual.
    // We've tried having a concrete EventListener and mocking the GetMethods to return an erroneous method,
    // but were unsuccessful due to us passing the type rather than the mockable instance. Mocking the retrieval of 
    // the type was once again impossible as it's unmanaged code. And a whole plethora of other options.
    // and we can't simply construct a type with an erroneous handler as this would brick the much more important test
    // case being DiscoverAndSubscribeAll
    // 
    // public void Subscribe_ShouldThrowException_WhenHandlerHasNoParameters()
   
}

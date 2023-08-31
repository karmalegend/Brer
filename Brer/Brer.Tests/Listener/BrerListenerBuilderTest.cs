using System;
using System.Collections.Generic;
using System.Reflection;
using Brer.Core.Interfaces;
using Brer.Listener;
using Brer.Listener.Runtime;
using Brer.Listener.Runtime.Interfaces;
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
        mockLogger.Received(1).Log(LogLevel.Information,"Subscribing MyTopic to System.Action`1[Brer.Listener.BrerListenerBuilder]");
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
        mockLogger.Received(1).Log(LogLevel.Information,"Subscribing MyTopic to Brer.Listener.Runtime.CallBackDispatcher`1[Brer.Listener.BrerListenerBuilder]");
        sut.Dispatchers.Keys.Should().Contain("MyTopic");
        sut.Dispatchers["MyTopic"].Should().BeOfType<CallBackDispatcher<BrerListenerBuilder>>();
    }
}

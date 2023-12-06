using System;
using System.Reflection;
using System.Reflection.Emit;
using Brer.Attributes;
using Brer.Core.Interfaces;
using Brer.Exceptions;
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

    [Fact]
    public void Subscribe_ShouldThrowInvalidBrerHandlerParameterCountException_WhenHandlerHasNoParameters()
    {
        // Arrange
        var mockBrerContext = Substitute.For<IBrerContext>();
        var mockLogger = Substitute.For<MockLogger<IBrerContext>>();
        mockBrerContext.Logger.Returns(mockLogger);

        var sut = new BrerListenerBuilder(mockBrerContext, Substitute.For<IServiceProvider>());

        var runtimeType = BuildDynamicTypeWithMethodsAndAttributes(0);

        // Act
        var exception = Record.Exception(() => sut.Subscribe(runtimeType))!;

        // Assert
        mockLogger.Received(1).Log(LogLevel.Information, "Subscribing DynamicTestType");
        exception.Should().BeOfType<InvalidBrerHandlerParameterCountException>();
        exception.Message.Should()
            .Be("Invalid number of parameters provide in handler: MethodWithNParameters, expected 1 but found 0");
    }


    [Fact]
    public void Subscribe_ShouldThrowInvalidBrerHandlerParameterCountException_WhenHandlerHasMoreThanOneParameter()
    {
        // Arrange
        var mockBrerContext = Substitute.For<IBrerContext>();
        var mockLogger = Substitute.For<MockLogger<IBrerContext>>();
        mockBrerContext.Logger.Returns(mockLogger);

        var sut = new BrerListenerBuilder(mockBrerContext, Substitute.For<IServiceProvider>());

        var runtimeType = BuildDynamicTypeWithMethodsAndAttributes(2);

        // Act
        var exception = Record.Exception(() => sut.Subscribe(runtimeType))!;

        // Assert
        mockLogger.Received(1).Log(LogLevel.Information, "Subscribing DynamicTestType");
        exception.Should().BeOfType<InvalidBrerHandlerParameterCountException>();
        exception.Message.Should()
            .Be("Invalid number of parameters provide in handler: MethodWithNParameters, expected 1 but found 2");
    }

    [Fact]
    public void Subscribe_ShouldSubscribeTypeWithHandlers_WhenTypeHasEventListenerAttributeAndHandlerAttributes()
    {
        // Arrange
        var mockBrerContext = Substitute.For<IBrerContext>();
        var mockLogger = Substitute.For<MockLogger<IBrerContext>>();
        var listenerWithHandlers = new BrerEventListenerWithHandlersMock().GetType();
        mockBrerContext.Logger.Returns(mockLogger);


        var sut = new BrerListenerBuilder(mockBrerContext, Substitute.For<IServiceProvider>());

        // Act
        sut.Subscribe(listenerWithHandlers);

        // Assert
        mockLogger.Received(1).Log(LogLevel.Information, "Subscribing BrerEventListenerWithHandlersMock");
        mockLogger.Received(1).Log(LogLevel.Information,
            "Subscribing BrerEventListenerWithHandlersMock EventHandler with param of type Object to MyUnitTestTopic");
        sut.Dispatchers.Keys.Count.Should().Be(1);
        sut.Dispatchers["MyUnitTestTopic"].Should().BeOfType<ListenerDispatcher>();
    }

    [Fact]
    public void DiscoverAndSubscribeAll_ShouldRegisterAndSubscribeClasses_WhenTheyHaveTheEventListenerAttribute()
    {
        // Arrange
        var mockBrerContext = Substitute.For<IBrerContext>();
        var mockLogger = Substitute.For<MockLogger<IBrerContext>>();

        mockBrerContext.Logger.Returns(mockLogger);

        var sut = new BrerListenerBuilder(mockBrerContext, Substitute.For<IServiceProvider>());

        // Act
        sut.DiscoverAndSubscribeAll();

        // Assert
        // Listener without handlers class
        mockLogger.Received(1).Log(LogLevel.Information, "Subscribing BrerEventListenerWithoutHandlersMock");

        // Initial Listener with handler class.
        mockLogger.Received(1).Log(LogLevel.Information, "Subscribing BrerEventListenerWithHandlersMock");
        mockLogger.Received(1).Log(LogLevel.Information,
            "Subscribing BrerEventListenerWithHandlersMock EventHandler with param of type Object to MyUnitTestTopic");

        // Additional Listener with handler class
        mockLogger.Received(1).Log(LogLevel.Information, "Subscribing ExtraBrerEventListenerWithHandlersMock");
        mockLogger.Received(1).Log(LogLevel.Information,
            "Subscribing ExtraBrerEventListenerWithHandlersMock ExtraEventHandler with param of type Object to MyExtraUnitTestTopic");

        sut.Dispatchers.Keys.Count.Should().Be(2);
        sut.Dispatchers["MyUnitTestTopic"].Should().BeOfType<ListenerDispatcher>();
        sut.Dispatchers["MyExtraUnitTestTopic"].Should().BeOfType<ListenerDispatcher>();
    }

    [Fact]
    public void Build_ShouldReturnABrerListener_WhenCalled()
    {
        // Arrange
        var mockBrerContext = Substitute.For<IBrerContext>();
        var mockLogger = Substitute.For<MockLogger<IBrerContext>>();
        mockBrerContext.Logger.Returns(mockLogger);


        var sut = new BrerListenerBuilder(mockBrerContext, Substitute.For<IServiceProvider>());

        // we just need a random Action
        Action<BrerListenerBuilder> callback = _ => { };

        // Act
        sut.Subscribe("MyTestTopic", callback); // ease of testing. could be refactored to use automatic discovery
        sut.Subscribe("MyTestTopic2", callback); // ease of testing. could be refactored to use automatic discovery
        var res = sut.Build();

        // Assert
        res.Should().BeOfType<BrerListener>();
        res.Topics.Should().BeEquivalentTo("MyTestTopic", "MyTestTopic2");
    }


    private static Type BuildDynamicTypeWithMethodsAndAttributes(int handlerParamCount)
    {
        var assemblyName = new AssemblyName("DynamicTestAssembly");
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule("DynamicTestModule");

        var typeBuilder = moduleBuilder.DefineType("DynamicTestType", TypeAttributes.Public);

        // Apply EventListenerAttribute at runtime
        // get the attribute from the existing assembly
        var eventListenerCtor = typeof(EventListenerAttribute).GetConstructor(Type.EmptyTypes);
        // create an instance of the attribute ¨builder" so we can apply it to a faked type.
        var eventListenerAttributeBuilder = new CustomAttributeBuilder(eventListenerCtor!, Array.Empty<object>());
        typeBuilder.SetCustomAttribute(eventListenerAttributeBuilder);

        var handlerParams = new Type[handlerParamCount];

        for (var i = 0; i < handlerParamCount; i++)
        {
            handlerParams[i] = typeof(string);
        }

        // Define method with HandlerAttribute with N parameters
        var methodBuilder = typeBuilder.DefineMethod("MethodWithNParameters", MethodAttributes.Public, typeof(void),
            handlerParams);

        // Get the constructor with the correct types. See GetConstructor docs for further info.
        var handlerAttributeCtor = typeof(HandlerAttribute).GetConstructor(new[] {typeof(string)});
        var handlerAttributeBuilder = new CustomAttributeBuilder(handlerAttributeCtor!, new object?[] {"SomeTopic"});
        methodBuilder.SetCustomAttribute(handlerAttributeBuilder);

        // all dynamic methods require a method body to be provided at the time of creation.
        // for this, we get an ILGenerator for the method and then use it to generate the method body using
        // MSIL instructions. In our case, we simply call .Emit(OpCodes.Ret), which adds an IL instruction representing
        // a "return" statement. Even if the method does nothing and has no return type (void), it still needs a return
        // statement at the end. This tells the runtime that the method's control flow has finished.
        var methodIlGen = methodBuilder.GetILGenerator();
        methodIlGen.Emit(OpCodes.Ret); // Add return to the method body

        return typeBuilder.CreateType()!;
    }
}

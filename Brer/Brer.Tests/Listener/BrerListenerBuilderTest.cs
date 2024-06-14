using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Brer.Attributes;
using Brer.Core.Interfaces;
using Brer.Exceptions;
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
    public void Subscribe_Should_Subscribe_Type_Without_Handlers_When_Type_Has_EventListener_Attribute_But_No_Handler_Attributes()
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
        
        var dispatchers = GetDispatchers(sut);
        dispatchers.Keys.Count.Should().Be(0);
    }
    
    [Fact]
    public void Subscribe_Should_Throw_Invalid_Brer_Handler_Parameter_Count_Exception_When_Handler_Has_No_Parameters()
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
    public void Subscribe_Should_Throw_Invalid_Brer_Handler_Parameter_Count_Exception_When_Handler_Has_More_Than_One_Parameter()
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
    public void Subscribe_Should_Subscribe_Type_With_Handlers_When_Type_Has_EventListener_Attribute_And_Handler_Attributes()
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
        
        var dispatchers = GetDispatchers(sut);

        dispatchers.Keys.Count.Should().Be(1);
        dispatchers["MyUnitTestTopic"].Should().BeOfType<ListenerDispatcher>();
    }

    [Fact]
    public void DiscoverAndSubscribeAll_Should_Register_And_Subscribe_Classes_When_They_Have_The_EventListener_Attribute()
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

        var dispatchers = GetDispatchers(sut);
        
        dispatchers.Keys.Count.Should().Be(2);
        dispatchers["MyUnitTestTopic"].Should().BeOfType<ListenerDispatcher>();
        dispatchers["MyExtraUnitTestTopic"].Should().BeOfType<ListenerDispatcher>();
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

    private static Dictionary<string, IDispatcher> GetDispatchers(BrerListenerBuilder listenerBuilder)
    {
        var prop = listenerBuilder.GetType().GetField("_dispatchers", BindingFlags.NonPublic | BindingFlags.Instance);
        var dispatchers = prop!.GetValue(listenerBuilder);
        return (dispatchers as Dictionary<string, IDispatcher>)!;
    }
}

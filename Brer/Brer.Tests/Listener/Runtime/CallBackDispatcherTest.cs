using System;
using System.Text;
using Brer.Listener.Runtime;
using BrerTests.Helpers;
using FluentAssertions;
using Newtonsoft.Json;
using NSubstitute;
using RabbitMQ.Client.Events;
using Xunit;

namespace BrerTests.Listener.Runtime
{
    public class CallBackDispatcherTest
    {
        [Fact]
        public void Dispatch_Valid_Event_Should_Invoke_Callback_When_Called()
        {
            // Arrange
            var callbackHelper = new CallbackHelper();
            var dummyData = new CallBackHelperEvent {EventName = "event"};

            var dispatcher = new CallBackDispatcher<CallBackHelperEvent>(callbackHelper.DoStuff);

            var eventArgs = new BasicDeliverEventArgs
            {
                Body = new ReadOnlyMemory<byte>(Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(dummyData)))
            };

            // Act
            dispatcher.Dispatch(eventArgs);

            // Assert
            callbackHelper.WasCalled.Should().BeTrue();
            callbackHelper.EventReceived.EventName.Should().Be("event");
        }

        [Fact]
        public void Dispatch_Invalid_Event_Should_Throw_InvalidOperationException_When_Called()
        {
            // Arrange
            var callbackMock = Substitute.For<Action<CallBackHelperEvent>>();
            var dispatcher = new CallBackDispatcher<CallBackHelperEvent>(callbackMock);

            var eventArgs = new BasicDeliverEventArgs
            {
                Body = new ReadOnlyMemory<byte>(
                    Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(null)))
            };

            // Act
            Action act = () => dispatcher.Dispatch(eventArgs);

            // Assert
            act.Should().Throw<InvalidOperationException>();
        }
    }
}

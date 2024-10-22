using System;
using System.Linq;
using System.Text;
using Brer.Core;
using FluentAssertions;
using NSubstitute;
using Xunit;
using Brer.Publisher;
using Brer.Core.Interfaces;
using BrerTests.Helpers;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace BrerTests.Publisher
{
    public class BrerPublisherTest
    {
        private readonly IBrerContext _context = Substitute.For<IBrerContext>();
        private readonly IModel _model = Substitute.For<IModel>();
        private readonly ILogger<IBrerContext> _logger;
        private readonly string _exchangeName = "ex";
        private readonly BrerPublisher _sut;

        public BrerPublisherTest()
        {
            _context.Connection.CreateModel().Returns(_model);
            _context.BrerOptions.Returns(new BrerOptions(Substitute.For<IConnectionFactory>(), _exchangeName, "q",null));
            _logger = Substitute.For<MockLogger<IBrerContext>>();
            _context.Logger.Returns(_logger);
            _sut = new BrerPublisher(_context);
        }

        [Fact]
        public void Publish_ShouldCall_CreateModel_And_ExchangeDeclare_When_Called()
        {
            // Arrange
            
            var topic = "topic";
            var message = "message";
            
            var properties = Substitute.For<IBasicProperties>();
            properties.ContentType.Returns(topic);

            _model.CreateBasicProperties().Returns(properties);

            // Act
            _sut.Publish(topic, message);

            // Assert
            _context.Connection.Received(1).CreateModel();
            _model.Received(1).ExchangeDeclare(_context.BrerOptions.ExchangeName, ExchangeType.Topic);
        }

        [Fact]
        public void Publish_ShouldLogInformation_When_Called()
        {
            // Arrange
            var topic = "topic";
            var message = "message";

            // Act
            _sut.Publish(topic, message);

            // Assert
            _context.Logger.Received(1).LogInformation(
                "Publishing event of topic {topic} with object {message} to exchange {exchangeName}",
                topic, message, _context.BrerOptions.ExchangeName);
        }

        [Fact]
        public void Publish_ShouldPublish_When_Called()
        {
            // Arrange
            var topic = "topic";
            var message = "message";
            var body = Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(message));
        
            // Act
            _sut.Publish(topic, message);
        
            // Assert
            _model.Received(1).BasicPublish(
                exchange: _context.BrerOptions.ExchangeName,
                routingKey: topic,
                mandatory: false,
                basicProperties: Arg.Any<IBasicProperties>(),
                Arg.Is<ReadOnlyMemory<byte>>(r => body.SequenceEqual(r.ToArray())));
        }
        
        [Fact]
        public void Publish_ShouldThrowException_When_Topic_Is_Null()
        {
            // Arrange
            string topic = null;
            var message = "message";
        
            // Act
            Action act = () => _sut.Publish(topic, message);
        
            // Assert
            act.Should().Throw<ArgumentNullException>();
        }
        
        
        [Fact]
        public void Publish_ShouldThrowException_When_Topic_Is_EmptyString()
        {
            // Arrange
            var topic = "";
            var message = "message";
        
            // Act
            Action act = () => _sut.Publish(topic, message);
        
            // Assert
            act.Should().Throw<ArgumentNullException>();
        }
        
        [Fact]
        public void Publish_ShouldThrowException_When_Topic_Is_WhiteSpaceString()
        {
            // Arrange
            var topic = "   ";
            var message = "message";
        
            // Act
            Action act = () => _sut.Publish(topic, message);
        
            // Assert
            act.Should().Throw<ArgumentNullException>();
        }
        
        [Fact]
        public void Publish_ShouldThrowException_When_Object_Is_Null()
        {
            // Arrange
            var topic = "topic";
            string message = null;
        
            // Act
            Action act = () => _sut.Publish(topic, message);
        
            // Assert
            act.Should().Throw<ArgumentNullException>();
        }
    }
}

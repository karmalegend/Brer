using System;
using Brer.Core;
using FluentAssertions;
using RabbitMQ.Client;
using Xunit;

namespace BrerTests.Core;

public class BrerOptionsBuilderTest
{
    private readonly BrerOptionsBuilder _sut;

    // ReSharper disable once ConvertConstructorToMemberInitializers
    // We want to reset this on every test hence we ignore the above mentioned rule.
    public BrerOptionsBuilderTest()
    {
        _sut = new BrerOptionsBuilder();
    }

    [Fact]
    public void Build_ShouldBuildTheBrerOptions_WhenBuildIsCalled()
    {
        // Arrange
        var expectedOptions =
            new BrerOptions(
                new ConnectionFactory {Port = 123, HostName = "host", UserName = "user", Password = "password"},
                ExchangeName: "Exchange", QueueName: "Queue");

        // Act
        var res = _sut.WithAddress(host: "host", port: 123).WithExchange("Exchange")
            .WithQueueName("Queue").WithUserName("user").WithPassword("password").Build();

        // Assert
        res.Should().BeEquivalentTo(expectedOptions);
    }

    [Fact]
    public void Build_ShouldBuildThebrerOptionsWithDefaultValues_WhenConstantsAreUsed()
    {
        // Arrange
        var expectedOptions =
            new BrerOptions(
                new ConnectionFactory {Port = 5672, HostName = "localhost", UserName = "guest", Password = "guest"},
                ExchangeName: "Exchange", QueueName: "Queue");

        // Act
        var res = _sut.WithAddress(host: BrerOptionsBuilder.LocalHost, BrerOptionsBuilder.DefaultPort)
            .WithPassword(BrerOptionsBuilder.DefaultLogin).WithUserName(BrerOptionsBuilder.DefaultLogin)
            .WithExchange("Exchange").WithQueueName("Queue").Build();

        // Assert
        res.Should().BeEquivalentTo(expectedOptions);
    }

    [Fact]
    public void ReadFromEnvironmentVariables_ShouldReadVarsFromEnvVars_WhenCalled()
    {
        // Arrange
        var expectedOptions =
            new BrerOptions(
                new ConnectionFactory {Port = 5672, HostName = "localhost", UserName = "guest", Password = "guest"},
                ExchangeName: "Exchange", QueueName: "Queue");

        Environment.SetEnvironmentVariable("BrerHostName", "Host", EnvironmentVariableTarget.Process);
        Environment.SetEnvironmentVariable("BrerPort", "5672", EnvironmentVariableTarget.Process);
        Environment.SetEnvironmentVariable("BrerExchangeName", "Exchange", EnvironmentVariableTarget.Process);
        Environment.SetEnvironmentVariable("BrerQueueName", "Queue", EnvironmentVariableTarget.Process);
        Environment.SetEnvironmentVariable("BrerUserName", "guest", EnvironmentVariableTarget.Process);
        Environment.SetEnvironmentVariable("BrerPassword", "guest", EnvironmentVariableTarget.Process);


        // Act
        var res = _sut.ReadFromEnvironmentVariables().Build();

        // Assert
        res.Should().BeEquivalentTo(expectedOptions);
    }


    [Theory]
    [InlineData(null, "42", "exchange", "queue", "user", "password", "Host")]
    [InlineData("Host", null, "exchange", "queue", "user", "password", "Port")]
    [InlineData("Host", "42", null, "queue", "user", "password", "ExchangeName")]
    [InlineData("Host", "42", "exchange", null, "user", "password", "QueueName")]
    [InlineData("Host", "42", "exchange", "queue", null, "password", "RabbitMqUser")]
    [InlineData("Host", "42", "exchange", "queue", "user", null, "RabbitMqPass")]
    public void ReadFromEnvironmentVariables_ShouldThrowArgumentNullException_WhenValueIsMissing(string host,
        string port, string exchange, string queue, string user, string password, string fieldNullName)
    {
        // Arrange
        Environment.SetEnvironmentVariable("BrerHostName", host, EnvironmentVariableTarget.Process);
        Environment.SetEnvironmentVariable("BrerPort", port, EnvironmentVariableTarget.Process);
        Environment.SetEnvironmentVariable("BrerExchangeName", exchange, EnvironmentVariableTarget.Process);
        Environment.SetEnvironmentVariable("BrerQueueName", queue, EnvironmentVariableTarget.Process);
        Environment.SetEnvironmentVariable("BrerUserName", user, EnvironmentVariableTarget.Process);
        Environment.SetEnvironmentVariable("BrerPassword", password, EnvironmentVariableTarget.Process);

        // Act
        Action act = () => { _sut.ReadFromEnvironmentVariables().Build(); };

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName(fieldNullName);
    }
}

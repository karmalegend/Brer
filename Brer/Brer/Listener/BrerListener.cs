using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Brer.Core.Interfaces;
using Brer.Helpers;
using Brer.Listener.Interfaces;
using Brer.Listener.Runtime.Interfaces;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Brer.Listener;

internal sealed class BrerListener : IBrerListener
{
    private readonly IBrerContext _context;
    private readonly Dictionary<string, IDispatcher> _dispatchers;
    private readonly Dictionary<string, IDispatcher> _wildCardDispatchers;
    private readonly string[] _topics;
    private IModel? _channel;

    public BrerListener(IBrerContext context, Dictionary<string, IDispatcher> dispatchers,
        Dictionary<string, IDispatcher> wildCardDispatchers)
    {
        _context = context;
        _dispatchers = dispatchers;
        _wildCardDispatchers = wildCardDispatchers;

        _topics = _dispatchers.Keys.Concat(_wildCardDispatchers.Keys).ToArray();
    }


    public IBrerListener StartListening()
    {
        _channel = _context.Connection.CreateModel();
        _channel.ExchangeDeclare(exchange: _context.BrerOptions.ExchangeName, type: ExchangeType.Topic);
        _channel.QueueDeclare(queue: _context.BrerOptions.QueueName, true, false, false);
        foreach (var topic in _topics)
        {
            _context.Logger.LogInformation(
                "Start Listening on queue {BrerQueue}, exchange {BrerExchange}, topic {BerTopic}",
                _context.BrerOptions.QueueName, _context.BrerOptions.ExchangeName, topic);
            _channel.QueueBind(queue: _context.BrerOptions.QueueName, exchange: _context.BrerOptions.ExchangeName,
                routingKey: topic);
        }

        TransformWildCardKeys();
        return this;
    }

    public IBrerListener StartReceivingEvents()
    {
        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += EventReceived;
        _channel.BasicConsume(queue: _context.BrerOptions.QueueName, autoAck: false, consumer: consumer);
        return this;
    }

    private void EventReceived(object? sender, BasicDeliverEventArgs e)
    {
        _context.Logger.LogInformation(
            "Received event on exchange {BrerExchange} with routing key {BrerRoutingKey}. Consumer tag: {BrerConsumerTag}",
            e.Exchange, e.RoutingKey, e.ConsumerTag);

        var topic = e.RoutingKey;
        if (_dispatchers.TryGetValue(topic, out var dispatcher))
        {
            FireAndForget(async () => await ProcessEvent(dispatcher, e));
        }
        else
        {
            var wildCardDispatcherTopic = _wildCardDispatchers.Keys.First(x => Regex.IsMatch(topic, x));
            FireAndForget(async () => await ProcessEvent(_wildCardDispatchers[wildCardDispatcherTopic], e));
        }
    }

    private async Task ProcessEvent(IDispatcher dispatcher, BasicDeliverEventArgs e)
    {
        try
        {
            await dispatcher.Dispatch(e);
            _channel?.BasicAck(e.DeliveryTag, false);
            _context.Logger.LogInformation(
                "Handled event on exchange {BrerExchange} with routing key {BrerRoutingKey}. Consumer tag: {BrerConsumerTag}",
                e.Exchange, e.RoutingKey, e.ConsumerTag);
        }
        catch (Exception exception)
        {
            _context.Logger.LogError(exception,
                "Failed to handle event on exchange {BrerExchange} with routing key {BrerRoutingKey}. Consumer tag: {BrerConsumerTag}",
                e.Exchange, e.RoutingKey, e.ConsumerTag);
            
            var headers = GenerateHeaders(e, exception);

            _channel?.BasicPublish(e.Exchange, e.RoutingKey, headers, e.Body);
            _channel?.BasicAck(e.DeliveryTag, false);
        }
    }

    private IBasicProperties? GenerateHeaders(BasicDeliverEventArgs e, Exception exception)
    {
        var requeueCount = 1;

        if (e.BasicProperties.Headers != null && e.BasicProperties.Headers.TryGetValue("x-Brer-RequeueCount", out var requeueCountObject) &&
            requeueCountObject != null)
        {
            requeueCount = Convert.ToInt32(requeueCountObject);
            requeueCount += 1;
        }

        var props = _channel?.CreateBasicProperties();
        if (props != null)
        {
            props.Headers = new Dictionary<string, object>
            {
                {"x-Brer-Exception", exception.GetType().ToString()},
                {"x-Brer-Exception-Message", exception.Message},
                {"x-Brer-Exception-StackTrace", exception.StackTrace ?? string.Empty},
                {"x-Brer-RequeueCount", requeueCount}
            };
        }

        return props;
    }

    private void FireAndForget(Func<Task> taskFunc)
    {
        Task.Run(async () =>
        {
            try
            {
                await taskFunc();
            }
            catch (Exception ex)
            {
                _context.Logger.LogError(ex, "Error in FireAndForget task");
            }
        });
    }
    
    private void TransformWildCardKeys()
    {
        _context.Logger.LogInformation("Transforming wildcard keys into pattern");
        var keys = _wildCardDispatchers.Keys.ToArray();
        foreach (var key in keys)
        {
            var pattern = key.Replace(".", "\\.").Replace("*", @"\w+").Replace("#", "[\\w\\.]+");
            _wildCardDispatchers.RenameKey(key, pattern);
        }
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _context.Dispose();
    }
}

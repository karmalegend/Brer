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

    public BrerListener(IBrerContext context, Dictionary<string, IDispatcher> dispatchers, Dictionary<string,IDispatcher> wildCardDispatchers)
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
            _context.Logger.LogInformation("Start Listening on queue {BrerQueue}, exchange {BrerExchange}, topic {BerTopic}",
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
        _context.Logger.LogInformation("Received event on exchange {BrerExchange} with routingkey {BrerRoutingKey}. Consumertag : {BerConsumerTag}",
            e.Exchange, e.RoutingKey, e.ConsumerTag);
        try
        {
            var topic = e.RoutingKey;
            if (_dispatchers.TryGetValue(topic, out var dispatcher))
            {
                var dispatcherTask = Task.Run(() => dispatcher.Dispatch(e));
                dispatcherTask.Wait();
            }
            else
            {
                var wildCardDispatcherTopic = _wildCardDispatchers.Keys.First(x => Regex.IsMatch(topic, x));
                _wildCardDispatchers[wildCardDispatcherTopic].Dispatch(e);
            }
            

            //only acknowledge when dispatch has successfully finished.
            _channel?.BasicAck(e.DeliveryTag, false);
            _context.Logger.LogInformation("Handled event on exchange {BrerExchange} with routingkey {BrerRoutingKey }. Consumertag : {BrerConsumerTag }",
                e.Exchange, e.RoutingKey, e.ConsumerTag);
        }
        catch (Exception exception)
        {
            // This puts the message back in the Queue
            // Note that if the event is somehow malformed or there's flawed logic in the application
            // this will infinitely add the message back to the queue.
            // this is ONLY meant to happen when a service breaking exception occurs.
            // rendering it unable to re-pop items from the queue.
            _channel?.BasicNack(e.DeliveryTag, false, true);
            _context.Logger.LogError(exception,
                "Failed to handle event on exchange {BrerExchange} with routingkey {BrerRoutingKey}. Consumertag : {BrerConsumerTag}",
                e.Exchange, e.RoutingKey, e.ConsumerTag);
        }
    }
    
    
    private void TransformWildCardKeys()
    {
        _context.Logger.LogInformation("Transforming wildcard keys into pattern");
        var keys = _wildCardDispatchers.Keys.ToArray();
        foreach (var key in keys)
        {
            var pattern = key.Replace(".","\\.").Replace("*", @"\w+").Replace("#","[\\w\\.]+");
            _wildCardDispatchers.RenameKey(key,pattern);
        }
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _context.Dispose();
    }
}

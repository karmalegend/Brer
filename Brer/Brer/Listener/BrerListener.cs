using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Brer.Core.Interfaces;
using Brer.Listener.Interfaces;
using Brer.Listener.Runtime.Interfaces;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Brer.Listener;

public sealed class BrerListener : IBrerListener
{
    private readonly IBrerContext _context;
    private readonly Dictionary<string, IDispatcher> _dispatchers;
    private IModel? _channel;

    public BrerListener(IBrerContext context, Dictionary<string, IDispatcher> dispatchers)
    {
        _context = context;
        _dispatchers = dispatchers;
    }

    public IEnumerable<string> Topics => _dispatchers.Keys;

    public IBrerListener StartListening()
    {
        _channel = _context.Connection.CreateModel();
        _channel.ExchangeDeclare(exchange: _context.BrerOptions.ExchangeName, type: ExchangeType.Topic);
        _channel.QueueDeclare(queue: _context.BrerOptions.QueueName, true, false, false);
        foreach (var topic in Topics)
        {
            _context.Logger.LogInformation("Start Listening on queue {q}, exchange {exh}, topic {top} ",
                _context.BrerOptions.QueueName, _context.BrerOptions.ExchangeName, topic);
            _channel.QueueBind(queue: _context.BrerOptions.QueueName, exchange: _context.BrerOptions.ExchangeName,
                routingKey: topic);
        }

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
        _context.Logger.LogInformation("Received event on exchange {ex} with routingkey {key}. Consumertag : {ct}",
            e.Exchange, e.RoutingKey, e.ConsumerTag);
        try
        {
            var topic = e.RoutingKey;
            var dispatcherTask = Task.Run(() => _dispatchers[topic].Dispatch(e));
            dispatcherTask.Wait();

            //only acknowledge when dispatch has successfully finished.
            _channel?.BasicAck(e.DeliveryTag, false);
            _context.Logger.LogInformation("Handled event on exchange {ex} with routingkey {key}. Consumertag : {ct}",
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
                "Failed to handle event on exchange {ex} with routingkey {key}. Consumertag : {ct}",
                e.Exchange, e.RoutingKey, e.ConsumerTag);
        }
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _context.Dispose();
    }
}

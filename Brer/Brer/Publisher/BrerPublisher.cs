using System;
using System.Text;
using Brer.Core.Interfaces;
using Brer.Publisher.Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace Brer.Publisher;

internal class BrerPublisher : IBrerPublisher
{
    private readonly IBrerContext _context;
//
    public BrerPublisher(IBrerContext context)
    {
        _context = context;
    }

    public void Publish<T>(string topic, T obj)
    {
        // caller safety we don't need to throw given type specification. more a safety net for developers.
        if (string.IsNullOrEmpty(topic) || string.IsNullOrWhiteSpace(topic)) throw new ArgumentNullException(nameof(topic)); 
        if (obj == null) throw new ArgumentNullException(nameof(obj));
        
        using IModel channel = _context.Connection.CreateModel();
        channel.ExchangeDeclare(
            exchange: _context.BrerOptions.ExchangeName,
            type: ExchangeType.Topic
        );

        _context.Logger.LogInformation("Publishing event of topic {top} with object {tobj} to exchange {ex}",
            topic, obj.ToString(), _context.BrerOptions.ExchangeName);

        byte[] body = Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(obj));

        var properties = channel.CreateBasicProperties();
        properties.ContentType = topic;
        channel.BasicPublish(
            exchange: _context.BrerOptions.ExchangeName,
            routingKey: topic,
            mandatory: false,
            basicProperties: properties,
            body: body
        );
    }
}

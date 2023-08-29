using RabbitMQ.Client.Events;

namespace Brer.Listener.Runtime.Interfaces;

internal interface IDispatcher
{
    void Dispatch(BasicDeliverEventArgs e);
}

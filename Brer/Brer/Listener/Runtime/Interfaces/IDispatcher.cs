using RabbitMQ.Client.Events;

namespace Brer.Listener.Runtime.Interfaces;

public interface IDispatcher
{
    void Dispatch(BasicDeliverEventArgs e);
}

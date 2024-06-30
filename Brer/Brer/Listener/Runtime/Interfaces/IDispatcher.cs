using System.Threading.Tasks;
using RabbitMQ.Client.Events;

namespace Brer.Listener.Runtime.Interfaces;

internal interface IDispatcher
{
    Task Dispatch(BasicDeliverEventArgs e);
}

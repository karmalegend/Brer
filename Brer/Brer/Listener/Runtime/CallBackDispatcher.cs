using System;
using System.Text;
using Brer.Listener.Runtime.Interfaces;
using Newtonsoft.Json;
using RabbitMQ.Client.Events;

namespace Brer.Listener.Runtime;

public class CallBackDispatcher<T> : IDispatcher
{
    private readonly Action<T> _callback;

    public CallBackDispatcher(Action<T> callback)
    {
        _callback = callback;
    }

    public void Dispatch(BasicDeliverEventArgs e)
    {
        var bodyString = Encoding.Unicode.GetString(e.Body.ToArray());
        T inflated = JsonConvert.DeserializeObject<T>(bodyString) ?? throw new InvalidOperationException();
        _callback.Invoke(inflated);

        // deserialize e.Body to string to object of type T
        // invoke callback with the object
    }
}

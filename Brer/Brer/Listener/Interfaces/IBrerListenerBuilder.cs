using System;
using Brer.Listener.Runtime;

namespace Brer.Listener.Interfaces;

internal interface IBrerListenerBuilder
{
    BrerListenerBuilder Subscribe<T>(string topic, Action<T> callback);
    BrerListenerBuilder Subscribe<T>(string topic, CallBackDispatcher<T> dispatcher);
    BrerListenerBuilder Subscribe(Type type);
    BrerListenerBuilder DiscoverAndSubscribeAll();
    BrerListener Build();
}

using System;
using Brer.Listener.Runtime;

namespace Brer.Listener.Interfaces;

internal interface IBrerListenerBuilder
{
    IBrerListenerBuilder Subscribe<T>(string topic, Action<T> callback);
    IBrerListenerBuilder Subscribe<T>(string topic, CallBackDispatcher<T> dispatcher);
    IBrerListenerBuilder Subscribe(Type type);
    IBrerListenerBuilder DiscoverAndSubscribeAll();
    IBrerListener Build();
}

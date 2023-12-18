using System;

namespace BrerTests.Helpers;

public class ListenerDispatcherTestHandler
{

    public void HandleThatThrows(ListenerDispatcherEventData @event)
    {
        throw new InvalidCastException("ListenerDispatcherTestHandler.HandleThatThrows");
    }

    public virtual void Handle(ListenerDispatcherEventData @event)
    {
        throw new NotImplementedException($"Handler reached with event-data: {@event.Data}"); // this is not how we want to test this
        // however given we dont strictly limit ourselves to eventlisteners registered in the DI container
        // it becomes quite a pain to test. This seems like a fair middle ground.
    }
}

public class ListenerDispatcherEventData
{
    public string Data { get; set; }
}

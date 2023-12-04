using Brer.Attributes;

namespace BrerTests.Helpers;

[EventListener]
public class BrerEventListenerWithHandlersMock 
{

    [Handler("MyUnitTestTopic")]
    public void EventHandler(object _)
    {
        // Method intentionally left empty.
    }
}

[EventListener]
public class ExtraBrerEventListenerWithHandlersMock
{
    [Handler("MyExtraUnitTestTopic")]
    public void ExtraEventHandler(object _)
    {
        // Method intentionally left empty.
    }
}

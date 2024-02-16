using System;

namespace BrerTests.Helpers;

public class CallbackHelper
{
    public CallBackHelperEvent EventReceived { get; set; }
    public bool WasCalled { get; set; }
    
    public Action<CallBackHelperEvent> DoStuff => callBackHelperEvent =>
    {
        EventReceived = callBackHelperEvent;
        WasCalled = true;
    };
}

public class CallBackHelperEvent
{
    public string EventName { get; set; }
}

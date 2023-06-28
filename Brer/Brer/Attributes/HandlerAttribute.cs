using System;

namespace Brer.Attributes;

public class HandlerAttribute : Attribute
{
    public string Topic { get; }

    public HandlerAttribute(string topic)
    {
        Topic = topic;
    }
}

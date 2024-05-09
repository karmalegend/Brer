using System;
using Brer.Attributes;

namespace Brer.Exceptions;

public class InvalidWildCardTopicFormatException : Exception
{
    public InvalidWildCardTopicFormatException() : base(
        $"Wild Card Topic must contain at least 1 wildcard and names can only contain alpha numeric characters . * #  must not end in a . (must adhere to '{WildCardHandlerAttribute.RegexHandler}'")
    {
    }

    public InvalidWildCardTopicFormatException(string message) : base(message)
    {
    }

    public InvalidWildCardTopicFormatException(string message, Exception inner) : base(message, inner)
    {
    }
}

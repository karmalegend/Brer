using System;

namespace Brer.Exceptions;

public class InvalidBrerHandlerParameterCountException : Exception
{
    public InvalidBrerHandlerParameterCountException() : base ("Invalid number of parameters provided. Only provide 1 when registering a handler.")
    {
    }

    public InvalidBrerHandlerParameterCountException(string message) : base(message)
    {
    }

    public InvalidBrerHandlerParameterCountException(string message, Exception inner) : base(message, inner)
    {
    }
}

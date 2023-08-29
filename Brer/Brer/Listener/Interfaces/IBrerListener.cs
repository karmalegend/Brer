using System;
using System.Collections.Generic;

namespace Brer.Listener.Interfaces;

internal interface IBrerListener : IDisposable
{
    IEnumerable<string> Topics { get; }

    IBrerListener StartListening();
    IBrerListener StartReceivingEvents();
}

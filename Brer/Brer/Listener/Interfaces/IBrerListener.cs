using System;
using System.Collections.Generic;

namespace Brer.Listener.Interfaces;

internal interface IBrerListener : IDisposable
{
    IBrerListener StartListening();
    IBrerListener StartReceivingEvents();
}

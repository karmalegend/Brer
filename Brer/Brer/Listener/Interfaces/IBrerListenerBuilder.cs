﻿using System;

namespace Brer.Listener.Interfaces;

internal interface IBrerListenerBuilder
{
    IBrerListenerBuilder Subscribe(Type type);
    IBrerListenerBuilder DiscoverAndSubscribeAll();
    IBrerListener Build();
}

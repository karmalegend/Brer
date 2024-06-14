using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Brer.Attributes;
using Brer.Core.Interfaces;
using Brer.Exceptions;
using Brer.Listener.Interfaces;
using Brer.Listener.Runtime;
using Brer.Listener.Runtime.Interfaces;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.Logging;

namespace Brer.Listener;

internal class BrerListenerBuilder : IBrerListenerBuilder
{
    private readonly IBrerContext _context;
    private readonly IServiceProvider _serviceProvider;

    private readonly Dictionary<string, IDispatcher> _dispatchers;
    private readonly Dictionary<string, IDispatcher> _wildCardDispatchers;
    private readonly Dictionary<string, IDispatcher> _fanoutDispatchers;

    public BrerListenerBuilder(IBrerContext context, IServiceProvider serviceProvider)
    {
        _context = context;
        _serviceProvider = serviceProvider;
        _dispatchers = new Dictionary<string, IDispatcher>();
        _wildCardDispatchers = new Dictionary<string, IDispatcher>();
        _fanoutDispatchers = new Dictionary<string, IDispatcher>();
    }

    public IBrerListenerBuilder Subscribe(Type type)
    {
        if (!IsEventListener(type)) return this;

        _context.Logger.LogInformation("Subscribing {Type}", type.Name);

        foreach (var method in type.GetMethods())
        {
            TrySubscribeMethod(type, method);
        }

        return this;
    }

    public IBrerListenerBuilder DiscoverAndSubscribeAll()
    {
        var referencedAssemblies = GetReferencingAssemblies();
        foreach (var assembly in referencedAssemblies)
        {
            foreach (var type in assembly.GetTypes())
                Subscribe(type);
        }

        return this;
    }

    public IBrerListener Build()
    {
        var listener = new BrerListener(_context, _dispatchers, _wildCardDispatchers, _fanoutDispatchers);
        return listener;
    }

    private bool IsEventListener(Type type)
    {
        return type.GetCustomAttributes().Any(t => t.GetType() == typeof(EventListenerAttribute));
    }

    private void TrySubscribeMethod(Type type, MethodInfo method)
    {
        var handlerAttr = method.GetCustomAttribute<HandlerAttribute>();
        var wildCardHandlerAttr = method.GetCustomAttribute<WildCardHandlerAttribute>();
        var fanoutHandlerAttr = method.GetCustomAttribute<FanoutHandlerAttribute>();

        if (handlerAttr != null || wildCardHandlerAttr != null || fanoutHandlerAttr != null)
        {
            ValidateParameters(method);

            var parameterType = method.GetParameters()[0].ParameterType;
            var dispatcher = new ListenerDispatcher(type, method, parameterType, _serviceProvider);

            SubscribeMethod(type, method, parameterType, dispatcher, handlerAttr, wildCardHandlerAttr,
                fanoutHandlerAttr);
        }
    }

    private static void ValidateParameters(MethodInfo method)
    {
        var parameters = method.GetParameters();
        if (parameters.Length != 1)
        {
            throw new InvalidBrerHandlerParameterCountException(
                $"Invalid number of parameters provide in handler: {method.Name}, expected 1 but found {parameters.Length}");
        }
    }

    private void SubscribeMethod(Type type, MethodInfo method, Type parameterType, ListenerDispatcher dispatcher,
        HandlerAttribute? handlerAttr, WildCardHandlerAttribute? wildCardHandlerAttr,
        FanoutHandlerAttribute? fanoutHandlerAttribute)
    {
        if (handlerAttr != null)
        {
            _dispatchers.Add(handlerAttr.Topic, dispatcher);
            LogSubscription(type, method, parameterType, handlerAttr.Topic);
        }
        else if (wildCardHandlerAttr != null)
        {
            _wildCardDispatchers.Add(wildCardHandlerAttr.TopicWildCard, dispatcher);
            LogSubscription(type, method, parameterType, wildCardHandlerAttr.TopicWildCard);
        }
        else if (fanoutHandlerAttribute != null)
        {
            _fanoutDispatchers.Add(parameterType.ToString().ToLower(), dispatcher);
            LogSubscription(type, method, parameterType, "Fanout");
        }
    }


    private void LogSubscription(Type type, MethodInfo method, Type parameterType, string topic)
    {
        _context.Logger.LogInformation(
            "Subscribing {Type} {Method} with param of type {Parameter} to {HandlerAttr}",
            type.Name, method.Name, parameterType.Name,
            topic);
    }


    private IEnumerable<Assembly> GetReferencingAssemblies()
    {
        var thisAssemblyName = GetType().Assembly.FullName;

        var result = from library in DependencyContext.Default?.RuntimeLibraries
            where library.Dependencies.Any(d => thisAssemblyName!.StartsWith(d.Name))
            select Assembly.Load(new AssemblyName(library.Name));
        return result;
    }
}

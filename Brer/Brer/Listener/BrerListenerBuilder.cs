using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Brer.Attributes;
using Brer.Core.Interfaces;
using Brer.Exceptions;
using Brer.Listener.Interfaces;
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

    public BrerListenerBuilder(IBrerContext context, IServiceProvider serviceProvider)
    {
        _context = context;
        _dispatchers = new Dictionary<string, IDispatcher>();
        _wildCardDispatchers = new Dictionary<string, IDispatcher>();
        _serviceProvider = serviceProvider;
    }
    
    public IBrerListenerBuilder Subscribe(Type type)
    {
        if (type.GetCustomAttributes().All(t => t.GetType() != typeof(EventListenerAttribute))) return this;
        _context.Logger.LogInformation("Subscribing {Type}", type.Name);
        foreach (var method in type.GetMethods())
        {
            var handlerAttr = method.GetCustomAttribute<HandlerAttribute>();
            var wildCardHandlerAttr = method.GetCustomAttribute<WildCardHandlerAttribute>();

            if (handlerAttr == null && wildCardHandlerAttr == null) continue;

            var parameters = method.GetParameters();
            if (parameters.Length != 1)
            {
                throw new InvalidBrerHandlerParameterCountException(
                    $"Invalid number of parameters provide in handler: {method.Name}, expected 1 but found {parameters.Length}");
            }

            var parameterType = parameters[0].ParameterType;
            var dispatcher = new ListenerDispatcher(type, method, parameterType, _serviceProvider);

            _context.Logger.LogInformation(
                "Subscribing {Type} {Method} with param of type {Parameter} to {HandlerAttr}",
                type.Name, method.Name, parameterType.Name, handlerAttr == null ? wildCardHandlerAttr!.TopicWildCard : handlerAttr.Topic);

            if(handlerAttr == null)
            {
                _wildCardDispatchers.Add(wildCardHandlerAttr!.TopicWildCard,dispatcher);
                continue;
            }
            
            _dispatchers.Add(handlerAttr.Topic,dispatcher);
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
        var listener = new BrerListener(_context, _dispatchers,_wildCardDispatchers);
        return listener;
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

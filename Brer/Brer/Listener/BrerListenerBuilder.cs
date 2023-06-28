using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Brer.Attributes;
using Brer.Core.Interfaces;
using Brer.Listener.Interfaces;
using Brer.Listener.Runtime;
using Brer.Listener.Runtime.Interfaces;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.Logging;

namespace Brer.Listener
{
    public class BrerListenerBuilder : IBrerListenerBuilder
    {
        private readonly IBrerContext _context;
        private readonly Dictionary<string, IDispatcher> _dispatchers;
        private readonly IServiceProvider _serviceProvider;

        public BrerListenerBuilder(IBrerContext context, IServiceProvider serviceProvider)
        {
            _context = context;
            _dispatchers = new Dictionary<string, IDispatcher>();
            _serviceProvider = serviceProvider;
        }

        public BrerListenerBuilder Subscribe<T>(string topic, Action<T> callback)
        {
            _context.Logger.LogInformation("Subscribing {Topic} to {Callback}",topic,callback);
            var dispatcher = new CallBackDispatcher<T>(callback);
            _dispatchers.Add(topic, dispatcher);
            return this;
        }

        public BrerListenerBuilder Subscribe<T>(string topic, CallBackDispatcher<T> dispatcher)
        {
            _context.Logger.LogInformation("Subscribing {Topic} to {Dispatcher}",topic);
            _dispatchers.Add(topic, dispatcher);
            return this;
        }

        public BrerListenerBuilder Subscribe(Type type)
        {
            if (type.GetCustomAttributes().All(t => t.GetType() != typeof(EventListenerAttribute))) return this;
            _context.Logger.LogInformation("Subscribing {Type}",type.Name);
            foreach(var method in type.GetMethods())
            {
                var handlerAttr = method.GetCustomAttribute<HandlerAttribute>();
                
                if (handlerAttr == null) continue;
                
                var parameterType = method.GetParameters().FirstOrDefault()?.ParameterType;
                var dispatcher = new ListenerDispatcher(type, method, parameterType,_serviceProvider);
                
                _context.Logger.LogInformation("Subscribing {type} {method} with param of type {parameter} to {handlerAttr}",
                    type.Name,method.Name,parameterType?.Name,handlerAttr.Topic);

                _dispatchers.Add(handlerAttr.Topic, dispatcher);
            }
            return this;
        }

        public BrerListenerBuilder DiscoverAndSubscribeAll()
        {
            var referencedAssemblies = GetReferencingAssemblies();
            foreach (var assembly in referencedAssemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    Subscribe(type);
                }
            }
            return this;
        }
        
        
        private IEnumerable<Assembly> GetReferencingAssemblies()
        {
            var thisAssemblyName = GetType().Assembly.FullName;
 
            var result = from library in DependencyContext.Default.RuntimeLibraries
                where library.Dependencies.Any(d => thisAssemblyName!.StartsWith(d.Name))
                select Assembly.Load(new AssemblyName(library.Name));
            return result;
        }


        public BrerListener Build()
        {
            var listener = new BrerListener(_context, _dispatchers);
            return listener;
        }
    }
}

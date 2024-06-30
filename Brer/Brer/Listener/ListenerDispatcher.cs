using System;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;
using Brer.Listener.Runtime.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using RabbitMQ.Client.Events;

namespace Brer.Listener;

internal class ListenerDispatcher : IDispatcher
{
    private readonly Type _eventListenerType;
    private readonly MethodInfo _method;
    private readonly Type _parameterType;
    private readonly IServiceProvider _serviceProvider;

    public ListenerDispatcher(Type eventListenerType, MethodInfo method, Type parameterType,
        IServiceProvider serviceProvider)
    {
        _eventListenerType = eventListenerType;
        _method = method;
        _parameterType = parameterType;
        _serviceProvider = serviceProvider;
    }

    public async Task Dispatch(BasicDeliverEventArgs e)
    {
        var param = JsonConvert.DeserializeObject(
            Encoding.Unicode.GetString(e.Body.ToArray()),
            _parameterType);

        using var scope = _serviceProvider.CreateScope();

        var instance = ActivatorUtilities.GetServiceOrCreateInstance(
            scope.ServiceProvider, _eventListenerType);

        if (_method.ReturnType == typeof(Task))
        {
            var task = (Task) _method.Invoke(instance, new[] {param})!;
            await task;
        }
        else
        {
            _method.Invoke(instance, new[] {param});
        }
    }
}

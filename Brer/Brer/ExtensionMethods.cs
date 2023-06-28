using Brer.Core;
using Brer.Core.Interfaces;
using Brer.Listener;
using Brer.Listener.Interfaces;
using Brer.Publisher;
using Brer.Publisher.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Brer;

public static class ExtensionMethods
{
    public static IServiceCollection UseBrer(this IServiceCollection services,
        BrerOptions options)
    {
        services.AddSingleton<IBrerContext>(opt =>
        {
            var logger = opt.GetRequiredService<ILogger<BrerContext>>();
            return new BrerContext(options, logger);
        });
        services.AddTransient<IBrerPublisher, BrerPublisher>();
        services.AddTransient<IBrerListenerBuilder, BrerListenerBuilder>();

        services.AddHostedService<BrerHostedService>();

        // all other dependencies that are necessary or useful
        return services;
    }
}

namespace Shared.Events;

using System.Text.Json;

using AWS.Lambda.Powertools.Parameters;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using SharedKernel.Events;

public static class StartupExtensions
{
    public static IServiceCollection AddEventInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var sharedSettings = new SharedSettings()
        {
            EventBusName = config["EVENT_BUS_NAME"],
            ServiceName = config["SERVICE_NAME"]
        };

        services.AddSingleton(Options.Create(sharedSettings));
        
        services.AddSingleton<IEventBus, EventBridgeEventBus>();
        
        return services;
    }
}
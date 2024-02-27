namespace Shared.Events;

using Amazon.EventBridge;
using Amazon.EventBridge.Model;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

public static class StartupExtensions
{
    public static IServiceCollection AddEventInfrastructure(this IServiceCollection services)
    {
        var eventBridgeClient = new AmazonEventBridgeClient();
        
        services.AddSingleton(eventBridgeClient);
        
        var sharedSettings = new SharedSettings()
        {
            EventBusName = Environment.GetEnvironmentVariable("EVENT_BUS_NAME"),
            ServiceName = Environment.GetEnvironmentVariable("SERVICE_NAME")
        };

        services.AddSingleton(Options.Create(sharedSettings));
        
        services.AddSingleton<IEventBus, EventBridgeEventBus>();
        
        return services;
    }
}
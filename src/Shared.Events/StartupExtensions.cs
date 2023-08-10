namespace Shared.Events;

using Amazon.EventBridge;
using Amazon.EventBridge.Model;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using SharedKernel.Events;

public static class StartupExtensions
{
    public static IServiceCollection AddEventInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var eventBridgeClient = new AmazonEventBridgeClient();

        var primingTasks = new List<Task>();
        primingTasks.Add(
            eventBridgeClient.DescribeEventBusAsync(
                new DescribeEventBusRequest
                {
                    Name = $"{Environment.GetEnvironmentVariable("EVENT_BUS_NAME")}{Environment.GetEnvironmentVariable("STACK_POSTFIX")}"
                }));

        Task.WaitAll(primingTasks.ToArray());
        
        services.AddSingleton(eventBridgeClient);
        
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
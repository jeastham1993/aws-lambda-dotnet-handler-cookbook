using System.Text.Json;
using AWS.Lambda.Powertools.Parameters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Shared;

public static class StartupExtensions
{
    public static IServiceCollection AddSharedInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var provider = ParametersManager.SsmProvider
            .WithMaxAge(TimeSpan.FromMinutes(5));

        var dataString = provider.Get(config["CONFIGURATION_PARAM_NAME"]);

        Console.WriteLine(dataString);

        services.AddSingleton<IFeatureFlags>(new FeatureFlags(JsonSerializer.Deserialize<Dictionary<string, object>>(dataString)));
        
        var sharedSettings = new SharedSettings()
        {
            EventBusName = config["TABLE_NAME"],
            ServiceName = config["SERVICE_NAME"]
        };

        services.AddSingleton(Options.Create(sharedSettings));
        
        services.AddSingleton<IEventBus, EventBridgeEventBus>();
        
        return services;
    }
}
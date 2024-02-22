namespace SharedKernel;

using System.Text.Json;

using AWS.Lambda.Powertools.Parameters;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using SharedKernel.Features;

public static class StartupExtensions
{
    public static IServiceCollection AddSharedInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        Console.WriteLine("Retrieving SSM parameter");
        
        var provider = ParametersManager.SsmProvider
            .WithMaxAge(TimeSpan.FromMinutes(5));

        var dataString = provider.Get(config["CONFIGURATION_PARAM_NAME"]);

        services.AddSingleton<IFeatureFlags>(new FeatureFlags(JsonSerializer.Deserialize<Dictionary<string, object>>(dataString)));
        
        return services;
    }
}
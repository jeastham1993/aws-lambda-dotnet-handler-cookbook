using Amazon.EventBridge;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;

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
        Console.WriteLine($"Retrieving SSM parameter: {config["CONFIGURATION_PARAM_NAME"]}");

        var client = new AmazonSimpleSystemsManagementClient();
        
        var response = client.GetParameterAsync(
            new GetParameterRequest
            {
                Name = config["CONFIGURATION_PARAM_NAME"],
            }).GetAwaiter().GetResult();
        
        Console.WriteLine("Retrieved");

        services.AddSingleton<IFeatureFlags>(new FeatureFlags(JsonSerializer.Deserialize<Dictionary<string, object>>(response.Parameter.Value)));
        services.AddSingleton(new AmazonEventBridgeClient());
        
        return services;
    }
}
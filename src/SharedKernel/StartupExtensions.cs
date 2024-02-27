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
    public static IServiceCollection AddFeatureFlags(this IServiceCollection services, IConfiguration config)
    {
        Console.WriteLine($"Retrieving SSM parameter: {config["CONFIGURATION_PARAM_NAME"]}");

        var client = new AmazonSimpleSystemsManagementClient();
        
        var response = client.GetParameterAsync(
            new GetParameterRequest
            {
                Name = config["CONFIGURATION_PARAM_NAME"],
            }).GetAwaiter().GetResult();
        
        Console.WriteLine("Retrieved");

        var features = JsonSerializer.Deserialize<Dictionary<string, object>>(response.Parameter.Value);

        if (features != null)
        {
            services.AddSingleton<IFeatureFlags>(new FeatureFlags(features));    
        }
        
        return services;
    }
}
namespace StockTrader.Infrastructure;

using Amazon.DynamoDBv2;
using Amazon.EventBridge;
using Amazon.EventBridge.Model;
using Amazon.XRay.Recorder.Core;
using Amazon.XRay.Recorder.Handlers.AwsSdk;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using StockTrader.Shared;

public static class StartupExtensions
{
    public static IServiceCollection AddSharedServices(this IServiceCollection services)
    {
        AWSSDKHandler.RegisterXRayForAllServices();
        
        var config = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();

        var infrastructureSettings = new InfrastructureSettings()
        {
            TableName = config["TABLE_NAME"],
            EventBusName = config["EVENT_BUS_NAME"],
            ServiceName = $"{config["POWERTOOLS_SERVICE_NAME"]}.{config["ENV"]}"
        };

        services.AddSingleton(Options.Create(infrastructureSettings));
        services.AddSingleton<IConfiguration>(config);
        
        var dynamoDbClient = new AmazonDynamoDBClient();
        var eventBridgeClient = new AmazonEventBridgeClient();

        var primingTasks = new List<Task>();
        primingTasks.Add(dynamoDbClient.DescribeTableAsync(infrastructureSettings.TableName));
        primingTasks.Add(eventBridgeClient.DescribeEventBusAsync(new DescribeEventBusRequest()
        {
            Name = infrastructureSettings.EventBusName
        }));

        Task.WaitAll(primingTasks.ToArray());


        services.AddSingleton(dynamoDbClient);
        services.AddSingleton(eventBridgeClient);
        
        services.AddSingleton<IStockRepository, StockRepository>();
        services.AddSingleton<IEventBus, EventBridgeEventBus>();

        return services;
    }
}

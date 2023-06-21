namespace StockTrader.Infrastructure;

using System.Text.Json;

using Amazon.DynamoDBv2;
using Amazon.EventBridge;
using Amazon.EventBridge.Model;
using Amazon.XRay.Recorder.Handlers.AwsSdk;

using AWS.Lambda.Powertools.Idempotency;
using AWS.Lambda.Powertools.Logging;
using AWS.Lambda.Powertools.Parameters;
using AWS.Lambda.Powertools.Parameters.SimpleSystemsManagement;

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

        var provider = ParametersManager.SsmProvider
            .WithMaxAge(TimeSpan.FromMinutes(5));

        var dataString = provider.Get(config["CONFIGURATION_PARAM_NAME"]);

        Console.WriteLine(dataString);

        services.AddSingleton<IFeatureFlags>(new FeatureFlags(JsonSerializer.Deserialize<Dictionary<string, object>>(dataString)));

        var infrastructureSettings = new InfrastructureSettings
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
        primingTasks.Add(
            eventBridgeClient.DescribeEventBusAsync(
                new DescribeEventBusRequest
                {
                    Name = infrastructureSettings.EventBusName
                }));

        Task.WaitAll(primingTasks.ToArray());

        services.AddSingleton(dynamoDbClient);
        services.AddSingleton(eventBridgeClient);

        services.AddSingleton<IStockRepository, StockRepository>();
        services.AddSingleton<IEventBus, EventBridgeEventBus>();

        var options = new IdempotencyOptionsBuilder()
            .WithThrowOnNoIdempotencyKey(true)
            .WithEventKeyJmesPath("[StockSymbol]")
            .Build();

        Idempotency.Configure(
            builder => builder
                .WithOptions(options)
                .UseDynamoDb(
                    storeBuilder =>
                        storeBuilder
                            .WithTableName(config["IDEMPOTENCY_TABLE_NAME"])
                            .WithDynamoDBClient(dynamoDbClient)));

        return services;
    }
}
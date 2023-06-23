using Shared;

namespace StockTrader.Infrastructure;

using System.Text.Json;

using Amazon.DynamoDBv2;
using Amazon.EventBridge;
using Amazon.EventBridge.Model;
using Amazon.XRay.Recorder.Handlers.AwsSdk;

using AWS.Lambda.Powertools.Idempotency;
using AWS.Lambda.Powertools.Parameters;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using StockTrader.Shared;

public static class StartupExtensions
{
    public static IServiceCollection AddSharedServices(this IServiceCollection services)
    {
        services.AddApplicationConfiguration();
        services.AddAwsSdks();
        
        services.AddSingleton<IStockRepository, StockRepository>();

        return services;
    }

    private static IServiceCollection AddApplicationConfiguration(this IServiceCollection services)
    {
        var config = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();

        var infrastructureSettings = new InfrastructureSettings
        {
            TableName = config["TABLE_NAME"],
        };

        services.AddSharedInfrastructure(config);
        services.AddSingleton(Options.Create(infrastructureSettings));
        services.AddSingleton<IConfiguration>(config);

        return services;
    }
    
    private static IServiceCollection AddAwsSdks(this IServiceCollection services)
    {
        AWSSDKHandler.RegisterXRayForAllServices();

        var dynamoDbClient = new AmazonDynamoDBClient();
        var eventBridgeClient = new AmazonEventBridgeClient();

        var primingTasks = new List<Task>();
        primingTasks.Add(dynamoDbClient.DescribeTableAsync(Environment.GetEnvironmentVariable("TABLE_NAME")));
        primingTasks.Add(
            eventBridgeClient.DescribeEventBusAsync(
                new DescribeEventBusRequest
                {
                    Name = Environment.GetEnvironmentVariable("EVENT_BUS_NAME")
                }));

        Task.WaitAll(primingTasks.ToArray());

        services.AddSingleton(dynamoDbClient);
        services.AddSingleton(eventBridgeClient);

        var options = new IdempotencyOptionsBuilder()
            .WithThrowOnNoIdempotencyKey(true)
            .WithEventKeyJmesPath("[StockSymbol]")
            .Build();

        Idempotency.Configure(
            builder => builder
                .WithOptions(options)
                .UseDynamoDb(storeBuilder => storeBuilder.WithTableName(Environment.GetEnvironmentVariable("IDEMPOTENCY_TABLE_NAME"))));
        
        return services;
    }
}
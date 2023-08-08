namespace StockTrader.Infrastructure;

using Amazon.DynamoDBv2;
using Amazon.EventBridge;
using Amazon.EventBridge.Model;
using Amazon.XRay.Recorder.Handlers.AwsSdk;

using AWS.Lambda.Powertools.Idempotency;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using SharedKernel;

using StockTrader.Core;
using StockTrader.Core.StockAggregate;
using StockTrader.Core.StockAggregate.Handlers;

public record SharedServiceOptions(bool SkipAppConfiguration = false, bool SkipAwsSdks = false, bool SkipRepository = false);

public static class StartupExtensions
{
    public static IServiceCollection AddSharedServices(this IServiceCollection services, SharedServiceOptions? options = null)
    {
        var postfix = Environment.GetEnvironmentVariable("STACK_POSTFIX");
        
        if (options is null)
        {
            options = new SharedServiceOptions();
        }

        services.AddSingleton<IStockPriceFeatures, StockPriceFeatures>();

        if (!options.SkipAppConfiguration)
        {
            services.AddApplicationConfiguration(postfix);
        }

        if (!options.SkipAwsSdks)
        {
            services.AddAwsSdks(postfix);   
        }

        if (!options.SkipRepository)
        {
            services.AddSingleton<IStockRepository, StockRepository>();    
        }
        
        services.AddSingleton<SetStockPriceHandler>();

        return services;
    }

    private static IServiceCollection AddApplicationConfiguration(this IServiceCollection services, string postfix)
    {
        var config = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();

        var infrastructureSettings = new InfrastructureSettings
        {
            TableName = $"{config["TABLE_NAME"]}{postfix}",
        };

        services.AddSharedInfrastructure(config);
        services.AddSingleton(Options.Create(infrastructureSettings));
        services.AddSingleton<IConfiguration>(config);

        return services;
    }
    
    private static IServiceCollection AddAwsSdks(this IServiceCollection services, string postfix)
    {
        AWSSDKHandler.RegisterXRayForAllServices();

        var dynamoDbClient = new AmazonDynamoDBClient();
        var eventBridgeClient = new AmazonEventBridgeClient();

        var primingTasks = new List<Task>();
        primingTasks.Add(dynamoDbClient.DescribeTableAsync($"{Environment.GetEnvironmentVariable("TABLE_NAME")}{postfix}"));
        primingTasks.Add(
            eventBridgeClient.DescribeEventBusAsync(
                new DescribeEventBusRequest
                {
                    Name = $"{Environment.GetEnvironmentVariable("EVENT_BUS_NAME")}{postfix}"
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
                .UseDynamoDb(storeBuilder => storeBuilder.WithTableName($"{Environment.GetEnvironmentVariable("IDEMPOTENCY_TABLE_NAME")}{postfix}")));
        
        return services;
    }
}
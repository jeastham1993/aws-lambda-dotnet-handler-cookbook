namespace StockTrader.Infrastructure;

using Amazon.DynamoDBv2;
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
        Console.WriteLine("Adding shared services");
        
        var postfix = Environment.GetEnvironmentVariable("STACK_POSTFIX");
        
        if (options is null)
        {
            options = new SharedServiceOptions();
        }

        services.AddSingleton<IStockPriceFeatures, StockPriceFeatures>();
        
        Console.WriteLine("Added stock price features");

        if (!options.SkipAppConfiguration)
        {
            Console.WriteLine("Adding app config");
            services.AddApplicationConfiguration(postfix);
        }

        if (!options.SkipAwsSdks)
        {
            Console.WriteLine("Adding SDK's");
            
            services.AddAwsSdks(postfix);   
        }

        if (!options.SkipRepository)
        {
            Console.WriteLine("Adding repo");
            
            services.AddSingleton<IStockRepository, StockRepository>();    
        }
        
        Console.WriteLine("Adding handler");
        
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
        //AWSSDKHandler.RegisterXRayForAllServices();

        var dynamoDbClient = new AmazonDynamoDBClient();

        services.AddSingleton(dynamoDbClient);

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
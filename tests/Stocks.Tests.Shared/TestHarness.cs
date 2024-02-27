using Amazon;
using Amazon.DynamoDBv2;
using Amazon.EventBridge;
using Amazon.Runtime.CredentialManagement;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shared.Events;
using SharedKernel.Features;
using StockTrader.Infrastructure;
using StockTrader.SetStockPriceHandler;
using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;

namespace Stocks.Tests.Shared;

public class TestHarness
{
    private readonly IServiceProvider _serviceProvider;

    public TestHarness(IFeatureFlags featureFlags)
    {
        Environment.SetEnvironmentVariable("POWERTOOLS_SERVICE_NAME", "test-stock-price");
        Environment.SetEnvironmentVariable("POWERTOOLS_TRACE_DISABLED", "true");
        
        var postfix = Environment.GetEnvironmentVariable("STACK_POSTFIX");
        
        var serviceCollection = new ServiceCollection();
        
        var config = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .AddJsonFile("./appsettings.json")
            .Build();

        var infrastructureSettings = new InfrastructureSettings
        {
            TableName = $"{config["TABLE_NAME"]}{postfix}",
        };
        
        serviceCollection.AddSingleton(Options.Create(infrastructureSettings));
        serviceCollection.AddSingleton<IConfiguration>(config);

        serviceCollection.AddSingleton(featureFlags);

        serviceCollection.AddSharedServices(new SharedServiceOptions(true, true));
        
        serviceCollection.AddSingleton<Function>();
        
        var chain = new CredentialProfileStoreChain();
        
        var region = Environment.GetEnvironmentVariable("AWS_REGION") ?? "eu-west-1";
        var endpoint = RegionEndpoint.GetBySystemName(region);
        
        if (chain.TryGetAWSCredentials("dev", out var awsCredentials))
        {
            serviceCollection.AddSingleton(new AmazonDynamoDBClient(awsCredentials, endpoint));
            serviceCollection.AddSingleton(new AmazonEventBridgeClient(awsCredentials, endpoint));
        }
        else
        {
            serviceCollection.AddSingleton(new AmazonDynamoDBClient(endpoint));
            serviceCollection.AddSingleton(new AmazonEventBridgeClient(endpoint));
        }

        serviceCollection.AddSingleton<IEventBus, EventBridgeEventBus>();

        _serviceProvider = serviceCollection.BuildServiceProvider();
    }

    public T GetService<T>()
    {
        return this._serviceProvider.GetRequiredService<T>();
    }
}
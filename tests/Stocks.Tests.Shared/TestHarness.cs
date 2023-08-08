using Amazon;
using Amazon.DynamoDBv2;
using Amazon.EventBridge;
using Amazon.Runtime.CredentialManagement;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SharedKernel;
using SharedKernel.Events;
using SharedKernel.Features;
using StockTrader.API.Endpoints;
using StockTrader.Infrastructure;

namespace Stocks.Tests.Shared;

public class TestHarness
{
    private IServiceProvider _serviceProvider;

    public TestHarness(IFeatureFlags featureFlags)
    {
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

        var sharedSettings = new SharedSettings
        {
            EventBusName = $"{config["EVENT_BUS_NAME"]}{postfix}",
            ServiceName = config["SERVICE_NAME"],
        };
        
        serviceCollection.AddSingleton(Options.Create(infrastructureSettings));
        serviceCollection.AddSingleton(Options.Create(sharedSettings));
        serviceCollection.AddSingleton<IConfiguration>(config);

        serviceCollection.AddSingleton(featureFlags);

        serviceCollection.AddSharedServices(new SharedServiceOptions(true, true));

        serviceCollection.AddSingleton<GetStockPriceEndpoint>();
        serviceCollection.AddSingleton<SetStockPriceEndpoint>();
        
        var chain = new CredentialProfileStoreChain();

        IAmazonDynamoDB dynamoDbClient = null;
        IAmazonEventBridge eventBridgeClient = null;
        
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
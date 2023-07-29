using Amazon;
using Amazon.DynamoDBv2;
using Amazon.EventBridge;
using Amazon.Runtime.CredentialManagement;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using SharedKernel;
using SharedKernel.Events;
using SharedKernel.Features;
using StockTrader.API.Endpoints;
using StockTrader.Infrastructure;

namespace Stocks.Tests.Shared;

public class TestHarness
{
    private IServiceProvider _serviceProvider;

    public TestHarness(IFeatureFlags featureFlags, bool useMocks = false)
    {
        var serviceCollection = new ServiceCollection();
        
        var config = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .AddJsonFile("./appsettings.json")
            .Build();

        var infrastructureSettings = new InfrastructureSettings
        {
            TableName = config["TABLE_NAME"],
        };

        var sharedSettings = new SharedSettings
        {
            EventBusName = config["EVENT_BUS_NAME"],
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
        
        var endpoint = RegionEndpoint.GetBySystemName("us-east-1");
        
        if (chain.TryGetAWSCredentials("dev", out var awsCredentials))
        {
            serviceCollection.AddSingleton(new AmazonDynamoDBClient(awsCredentials, endpoint));
            serviceCollection.AddSingleton(new AmazonEventBridgeClient(awsCredentials, endpoint));
        }
        else
        {
            throw new Exception("Need a profile named dev");
        }
        
        serviceCollection.AddSingleton<IEventBus, EventBridgeEventBus>();

        _serviceProvider = serviceCollection.BuildServiceProvider();
    }

    public T GetService<T>()
    {
        return this._serviceProvider.GetRequiredService<T>();
    }
}
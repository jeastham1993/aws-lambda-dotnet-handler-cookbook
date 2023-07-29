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
using StockTrader.Core.StockAggregate;
using StockTrader.Infrastructure;

namespace Stocks.Tests.Shared;

public class MockTestHarness
{
    private IServiceProvider _serviceProvider;
    
    public Mock<IStockRepository> MockStockRepository { get; private set; }
    public Mock<IEventBus> MockEventBus { get; private set; }

    public MockTestHarness(IFeatureFlags featureFlags, bool useMocks = false)
    {
        var serviceCollection = new ServiceCollection();
        
        // Arrange
        MockStockRepository = new Mock<IStockRepository>();
        MockStockRepository.Setup(p => p.UpdateStock(It.IsAny<Stock>())).Verifiable();
        MockEventBus = new Mock<IEventBus>();
        MockEventBus.Setup(p => p.Publish(It.IsAny<Event>())).Verifiable();
        
        serviceCollection.AddSharedServices(new SharedServiceOptions(true, true, true));

        serviceCollection.AddSingleton(MockStockRepository.Object);
        serviceCollection.AddSingleton(MockEventBus.Object);
        serviceCollection.AddSingleton(featureFlags);
        serviceCollection.AddSingleton<SetStockPriceEndpoint>();

        _serviceProvider = serviceCollection.BuildServiceProvider();
    }

    public T GetService<T>()
    {
        return this._serviceProvider.GetRequiredService<T>();
    }
}
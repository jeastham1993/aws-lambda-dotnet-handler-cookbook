using Microsoft.Extensions.DependencyInjection;
using FakeItEasy;
using Shared.Events;
using SharedKernel.Features;
using StockTrader.Core.StockAggregate;
using StockTrader.Infrastructure;
using StockTrader.SetStockPriceHandler;

namespace Stocks.Tests.Shared;

public class MockTestHarness
{
    private readonly IServiceProvider _serviceProvider;
    
    public IStockRepository MockStockRepository { get; private set; }
    public IEventBus MockEventBus { get; private set; }

    public MockTestHarness(IFeatureFlags featureFlags)
    {
        var serviceCollection = new ServiceCollection();
        
        // Arrange
        MockStockRepository = A.Fake<IStockRepository>();
        
        MockEventBus = A.Fake<IEventBus>();

        serviceCollection.AddSharedServices(new SharedServiceOptions(true, true, true));

        serviceCollection.AddSingleton(MockStockRepository);
        serviceCollection.AddSingleton(MockEventBus);
        serviceCollection.AddSingleton(featureFlags);
        serviceCollection.AddSingleton<Function>();

        _serviceProvider = serviceCollection.BuildServiceProvider();
    }

    public T GetService<T>() where T : notnull
    {
        return this._serviceProvider.GetRequiredService<T>();
    }
}
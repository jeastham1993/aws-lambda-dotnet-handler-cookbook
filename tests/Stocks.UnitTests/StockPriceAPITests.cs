using Stocks.Tests.Shared;
using StockTrader.API.Endpoints;
using StockTrader.Infrastructure;

using System.Text.Json;
using Amazon.Lambda.Core;

using StockTrader.Core.StockAggregate;
using StockTrader.Core.StockAggregate.Handlers;

namespace Stocks.UnitTests;

public class SetStockPriceTests
{
    public SetStockPriceTests()
    {
        Environment.SetEnvironmentVariable("POWERTOOLS_METRICS_NAMESPACE", "pricing");
        Environment.SetEnvironmentVariable("POWERTOOLS_TRACE_DISABLED", "true");
    }
    
    [Fact]
    public async Task CanSetStockPrice_WhenRequestIsValid_ShouldStoreAndPublishEvent()
    {
        var mockFeatureFlags = FeatureFlagMocks.Default;

        var testHarness = new MockTestHarness(mockFeatureFlags);

        var function = testHarness.GetService<SetStockPriceEndpoint>();

        var testRequest = new SetStockPriceRequest()
        {
            NewPrice = 100,
            StockSymbol = "AMZ"
        };

        // Act
        var result = await function.SetStockPrice(testRequest, A.Fake<ILambdaContext>());
        
        // Assert
        result.StatusCode.Should().Be(200);

        var response = JsonSerializer.Deserialize<ApiWrapper<SetStockPriceResponse>>(result.Body);
        response?.Data.StockSymbol.Should().Be("AMZ");
        response?.Data.Price.Should().Be(100);

        A.CallTo(() => testHarness.MockStockRepository.UpdateStock(A<Stock>._)).MustHaveHappened();
    }
    
    [Fact]
    public async Task CanSetStockPrice_When10PercentIncreaseFeatureFlagEnabled_ShouldStoreAndPublishEvent()
    {
        var mockFeatureFlags = FeatureFlagMocks.IncreaseStockPriceEnabled;

        var testHarness = new MockTestHarness(mockFeatureFlags);

        var function = testHarness.GetService<SetStockPriceEndpoint>();

        var testRequest = new SetStockPriceRequest()
        {
            NewPrice = 100,
            StockSymbol = "AMZ"
        };

        // Act
        var result = await function.SetStockPrice(testRequest, A.Fake<ILambdaContext>());
        
        // Assert
        result.StatusCode.Should().Be(200);

        var response = JsonSerializer.Deserialize<ApiWrapper<SetStockPriceResponse>>(result.Body);
        response?.Data.StockSymbol.Should().Be("AMZ");
        response?.Data.Price.Should().Be(110);

        A.CallTo(() => testHarness.MockStockRepository.UpdateStock(A<Stock>._)).MustHaveHappened();
    }
    
    [Fact]
    public async Task CanSetStockPrice_WhenReduceStockPriceFeatureFlagEnabled_ShouldStoreAndPublishEvent()
    {
        var mockFeatureFlags = FeatureFlagMocks.ReduceStockPriceEnabled;

        var testHarness = new MockTestHarness(mockFeatureFlags);

        var function = testHarness.GetService<SetStockPriceEndpoint>();

        var testRequest = new SetStockPriceRequest()
        {
            NewPrice = 100,
            StockSymbol = "AMZ"
        };

        // Act
        var result = await function.SetStockPrice(testRequest, A.Fake<ILambdaContext>());
        
        // Assert
        result.StatusCode.Should().Be(200);

        var response = JsonSerializer.Deserialize<ApiWrapper<SetStockPriceResponse>>(result.Body);
        response?.Data.StockSymbol.Should().Be("AMZ");
        response?.Data.Price.Should().Be(50);

        A.CallTo(() => testHarness.MockStockRepository.UpdateStock(A<Stock>._)).MustHaveHappenedOnceExactly();
    }
    
    [Fact]
    public async Task CanSetStockPrice_WhenNewStockPriceIsZero_ShouldReturnBadRequest()
    {
        var mockFeatureFlags = FeatureFlagMocks.Default;

        var testHarness = new MockTestHarness(mockFeatureFlags);

        var function = testHarness.GetService<SetStockPriceEndpoint>();

        var testRequest = new SetStockPriceRequest()
        {
            NewPrice = 0,
            StockSymbol = "AMZ"
        };

        // Act
        var result = await function.SetStockPrice(testRequest, A.Fake<ILambdaContext>());
        
        // Assert
        result.StatusCode.Should().Be(400);

        A.CallTo(() => testHarness.MockStockRepository.UpdateStock(A<Stock>._)).MustNotHaveHappened();
    }
}
using System.Text.Json;
using Amazon.Lambda.Core;
using SharedKernel.Features;
using Stocks.Tests.Shared;
using StockTrader.Core.StockAggregate.Handlers;
using StockTrader.Infrastructure;
using StockTrader.SetStockPriceHandler;

namespace Stocks.IntegrationTests;

public class SetStockPriceTests
{
    public SetStockPriceTests()
    {
        Environment.SetEnvironmentVariable("POWERTOOLS_TRACE_DISABLED", "true");
    }

    [Fact]
    public async Task CanSetStockPrice_WhenRequestIsValid_ShouldStoreAndPublishEvent()
    {
        var mockFeatureFlags = A.Fake<IFeatureFlags>();

        A.CallTo(
                () => mockFeatureFlags.Evaluate(
                    A<string>.Ignored,
                    A<Dictionary<string, object>>.Ignored,
                    A<object>.Ignored))
            .Returns("False");
        
        var testHarness = new TestHarness(mockFeatureFlags);
        
        var setStockPriceEndpoint = testHarness.GetService<Function>();
        
        var testRequest = new SetStockPriceRequest()
        {
            NewPrice = 100,
            StockSymbol = "AMZ"
        };

        // Act
        var result = await setStockPriceEndpoint.SetStockPrice(testRequest);
        
        // Assert
        result.StatusCode.Should().Be(200);

        var response = JsonSerializer.Deserialize<ApiWrapper<SetStockPriceResponse>>(result.Body);
        response?.Data.StockSymbol.Should().Be("AMZ");
        response?.Data.Price.Should().Be(100);
    }
    
    [Fact]
    public async Task CanSetStockPrice_When10PercentIncreaseFeatureFlagEnabled_ShouldStoreAndPublishEvent()
    {
        var mockFeatureFlags = A.Fake<IFeatureFlags>();

        A.CallTo(
                () => mockFeatureFlags.Evaluate(
                    A<string>._,
                    A<Dictionary<string, object>>._,
                    A<object>._))
            .Returns("False");

        A.CallTo(
                () => mockFeatureFlags.Evaluate(
                    "ten_percent_share_increase",
                    A<Dictionary<string, object>>._,
                    A<object>._))
            .Returns("True");
        
        var testHarness = new TestHarness(mockFeatureFlags);
        
        var setStockPriceEndpoint = testHarness.GetService<Function>();

        var testRequest = new SetStockPriceRequest()
        {
            NewPrice = 100,
            StockSymbol = "AMZ"
        };

        // Act
        var result = await setStockPriceEndpoint.SetStockPrice(testRequest);
        
        // Assert
        result.StatusCode.Should().Be(200);

        var response = JsonSerializer.Deserialize<ApiWrapper<SetStockPriceResponse>>(result.Body);
        response?.Data.StockSymbol.Should().Be("AMZ");
        response?.Data.Price.Should().Be(110);
    }
    
    [Fact]
    public async Task CanSetStockPrice_WhenReduceStockPriceFeatureFlagEnabled_ShouldStoreAndPublishEvent()
    {
        var mockFeatureFlags = FeatureFlagMocks.ReduceStockPriceEnabled;

        var testRequest = new SetStockPriceRequest()
        {
            NewPrice = 100,
            StockSymbol = "AMZ"
        };
        
        var testHarness = new TestHarness(mockFeatureFlags);
        
        var setStockPriceEndpoint = testHarness.GetService<Function>();

        // Act
        var result = await setStockPriceEndpoint.SetStockPrice(testRequest);
        
        // Assert
        result.StatusCode.Should().Be(200);

        var response = JsonSerializer.Deserialize<ApiWrapper<SetStockPriceResponse>>(result.Body);
        response?.Data.StockSymbol.Should().Be("AMZ");
        response?.Data.Price.Should().Be(50);
    }
    
    [Fact]
    public async Task CanSetStockPrice_WhenNewStockPriceIsZero_ShouldReturnBadRequest()
    {
        var mockFeatureFlags = FeatureFlagMocks.Default;
        
        var testRequest = new SetStockPriceRequest()
        {
            NewPrice = 0,
            StockSymbol = "AMZ"
        };
        
        var testHarness = new TestHarness(mockFeatureFlags);
        
        var setStockPriceEndpoint = testHarness.GetService<Function>();

        // Act
        var result = await setStockPriceEndpoint.SetStockPrice(testRequest);
        
        // Assert
        result.StatusCode.Should().Be(400);
    }
}
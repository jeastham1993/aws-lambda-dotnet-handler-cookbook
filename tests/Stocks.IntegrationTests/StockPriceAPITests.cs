using System.Text.Json;
using Amazon.Lambda.Core;
using SharedKernel.Features;
using Stocks.Tests.Shared;
using StockTrader.API.Endpoints;
using StockTrader.Core.StockAggregate;
using StockTrader.Core.StockAggregate.Handlers;
using StockTrader.Infrastructure;

namespace Stocks.IntegrationTests;

public class SetStockPriceTests
{
    public SetStockPriceTests()
    {
        Environment.SetEnvironmentVariable("POWERTOOLS_METRICS_NAMESPACE", "test-stock-price");
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
        
        var setStockPriceEndpoint = testHarness.GetService<SetStockPriceEndpoint>();
        
        var testRequest = new SetStockPriceRequest()
        {
            NewPrice = 100,
            StockSymbol = "AMZ"
        };

        // Act
        var result = await setStockPriceEndpoint.SetStockPrice(testRequest, A.Fake<ILambdaContext>());
        
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
        
        var setStockPriceEndpoint = testHarness.GetService<SetStockPriceEndpoint>();

        var testRequest = new SetStockPriceRequest()
        {
            NewPrice = 100,
            StockSymbol = "AMZ"
        };

        // Act
        var result = await setStockPriceEndpoint.SetStockPrice(testRequest, A.Fake<ILambdaContext>());
        
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
        
        var setStockPriceEndpoint = testHarness.GetService<SetStockPriceEndpoint>();

        // Act
        var result = await setStockPriceEndpoint.SetStockPrice(testRequest, A.Fake<ILambdaContext>());
        
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
        
        var setStockPriceEndpoint = testHarness.GetService<SetStockPriceEndpoint>();

        // Act
        var result = await setStockPriceEndpoint.SetStockPrice(testRequest, A.Fake<ILambdaContext>());
        
        // Assert
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task CanGetStockPrice_WhenRequestIsValid_ShouldRetrieveStock()
    {
        var mockFeatureFlags = FeatureFlagMocks.Default;
        
        var testHarness = new TestHarness(mockFeatureFlags);
        
        var getStockPriceEndpoint = testHarness.GetService<GetStockPriceEndpoint>();
        var setStockPriceEndpoint = testHarness.GetService<SetStockPriceEndpoint>();

        var testStockSymbol = Guid.NewGuid().ToString();
        
        await setStockPriceEndpoint.SetStockPrice(new SetStockPriceRequest(){StockSymbol = testStockSymbol, NewPrice = 100}, A.Fake<ILambdaContext>());

        // Act
        var result = await getStockPriceEndpoint.GetStockPrice(testStockSymbol);
        
        // Assert
        result.StatusCode.Should().Be(200);

        var response = JsonSerializer.Deserialize<ApiWrapper<StockDTO>>(result.Body);
        response?.Data.StockSymbol.Should().Be(testStockSymbol);
    }

    [Fact]
    public async Task CanGetStockPrice_WhenRequestIsForUnknownStockCode_ShouldReturn404()
    {
        var mockFeatureFlags = FeatureFlagMocks.Default;
        
        var testHarness = new TestHarness(mockFeatureFlags);
        
        var getStockPriceEndpoint = testHarness.GetService<GetStockPriceEndpoint>();

        var testStockSymbol = Guid.NewGuid().ToString();

        // Act
        var result = await getStockPriceEndpoint.GetStockPrice(testStockSymbol);
        
        // Assert
        result.StatusCode.Should().Be(404);
    }
}
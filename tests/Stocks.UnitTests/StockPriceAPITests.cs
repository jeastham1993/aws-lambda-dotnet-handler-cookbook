using Stocks.Tests.Shared;
using StockTrader.API.Endpoints;

namespace Stocks.UnitTests;

using System.Text.Json;
using Amazon.Lambda.Core;

using SharedKernel.Features;

using StockTrader.Core.StockAggregate;
using StockTrader.Core.StockAggregate.Events;
using StockTrader.Core.StockAggregate.Handlers;

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
        var mockFeatureFlags = new Mock<IFeatureFlags>();
        mockFeatureFlags.Setup(
            p => p.Evaluate(
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<object>()))
            .Returns("False");

        var testHarness = new MockTestHarness(mockFeatureFlags.Object);

        var function = testHarness.GetService<SetStockPriceEndpoint>();

        var testRequest = new SetStockPriceRequest()
        {
            NewPrice = 100,
            StockSymbol = "AMZ"
        };

        // Act
        var result = await function.SetStockPrice(testRequest, new Mock<ILambdaContext>().Object);
        
        // Assert
        result.StatusCode.Should().Be(200);

        var response = JsonSerializer.Deserialize<SetStockPriceResponse>(result.Body);
        response?.StockSymbol.Should().Be("AMZ");
        response?.Price.Should().Be(100);
        
        testHarness.MockEventBus.Verify(p => p.Publish(It.IsAny<StockPriceUpdatedV1Event>()), Times.Once);
        testHarness.MockStockRepository.Verify(p => p.UpdateStock(It.IsAny<Stock>()), Times.Once);
    }
    
    [Fact]
    public async Task CanSetStockPrice_When10PercentIncreaseFeatureFlagEnabled_ShouldStoreAndPublishEvent()
    {
        var mockFeatureFlags = new Mock<IFeatureFlags>();
        mockFeatureFlags.Setup(
                p => p.Evaluate(
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, object>>(),
                    It.IsAny<object>()))
            .Returns("False");
        mockFeatureFlags.Setup(
                p => p.Evaluate(
                    "ten_percent_share_increase",
                    It.IsAny<Dictionary<string, object>>(),
                    It.IsAny<object>()))
            .Returns("True");

        var testHarness = new MockTestHarness(mockFeatureFlags.Object);

        var function = testHarness.GetService<SetStockPriceEndpoint>();

        var testRequest = new SetStockPriceRequest()
        {
            NewPrice = 100,
            StockSymbol = "AMZ"
        };

        // Act
        var result = await function.SetStockPrice(testRequest, new Mock<ILambdaContext>().Object);
        
        // Assert
        result.StatusCode.Should().Be(200);

        var response = JsonSerializer.Deserialize<SetStockPriceResponse>(result.Body);
        response?.StockSymbol.Should().Be("AMZ");
        response?.Price.Should().Be(110);
        
        testHarness.MockEventBus.Verify(p => p.Publish(It.IsAny<StockPriceUpdatedV1Event>()), Times.Once);
        testHarness.MockStockRepository.Verify(p => p.UpdateStock(It.IsAny<Stock>()), Times.Once);
    }
    
    [Fact]
    public async Task CanSetStockPrice_WhenReduceStockPriceFeatureFlagEnabled_ShouldStoreAndPublishEvent()
    {
        var mockFeatureFlags = new Mock<IFeatureFlags>();
        mockFeatureFlags.Setup(
                p => p.Evaluate(
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, object>>(),
                    It.IsAny<object>()))
            .Returns("False");
        mockFeatureFlags.Setup(
                p => p.Evaluate(
                    "reduce_stock_price_for_company",
                    It.IsAny<Dictionary<string, object>>(),
                    It.IsAny<object>()))
            .Returns("True");

        var testHarness = new MockTestHarness(mockFeatureFlags.Object);

        var function = testHarness.GetService<SetStockPriceEndpoint>();

        var testRequest = new SetStockPriceRequest()
        {
            NewPrice = 100,
            StockSymbol = "AMZ"
        };

        // Act
        var result = await function.SetStockPrice(testRequest, new Mock<ILambdaContext>().Object);
        
        // Assert
        result.StatusCode.Should().Be(200);

        var response = JsonSerializer.Deserialize<SetStockPriceResponse>(result.Body);
        response?.StockSymbol.Should().Be("AMZ");
        response?.Price.Should().Be(50);
        
        testHarness.MockEventBus.Verify(p => p.Publish(It.IsAny<StockPriceUpdatedV1Event>()), Times.Once);
        testHarness.MockStockRepository.Verify(p => p.UpdateStock(It.IsAny<Stock>()), Times.Once);
    }
    
    [Fact]
    public async Task CanSetStockPrice_WhenNewStockPriceIsZero_ShouldReturnBadRequest()
    {
        var mockFeatureFlags = new Mock<IFeatureFlags>();
        mockFeatureFlags.Setup(
                p => p.Evaluate(
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, object>>(),
                    It.IsAny<object>()))
            .Returns("False");

        var testHarness = new MockTestHarness(mockFeatureFlags.Object);

        var function = testHarness.GetService<SetStockPriceEndpoint>();

        var testRequest = new SetStockPriceRequest()
        {
            NewPrice = 0,
            StockSymbol = "AMZ"
        };

        // Act
        var result = await function.SetStockPrice(testRequest, new Mock<ILambdaContext>().Object);
        
        // Assert
        result.StatusCode.Should().Be(400);
        
        testHarness.MockEventBus.Verify(p => p.Publish(It.IsAny<StockPriceUpdatedV1Event>()), Times.Never);
        testHarness.MockStockRepository.Verify(p => p.UpdateStock(It.IsAny<Stock>()), Times.Never);
    }
}
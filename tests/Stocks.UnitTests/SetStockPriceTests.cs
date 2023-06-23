using Shared.Events;
using Shared.Features;

namespace Stocks.UnitTests;

using System.Text.Json;
using Amazon.Lambda.Core;
using SetStockPriceFunction;
using StockTrader.Shared;

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
        // Arrange
        var stockRepository = new Mock<IStockRepository>();
        stockRepository.Setup(p => p.UpdateStock(It.IsAny<Stock>())).Verifiable();
        var mockEventBus = new Mock<IEventBus>();
        mockEventBus.Setup(p => p.Publish(It.IsAny<Event>())).Verifiable();
        
        var mockFeatureFlags = new Mock<IFeatureFlags>();
        mockFeatureFlags.Setup(
            p => p.Evaluate(
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<object>()))
            .Returns("False");

        var handler = new SetStockPriceHandler(
            stockRepository.Object,
            mockEventBus.Object,
            mockFeatureFlags.Object);
        
        var function = new Function(handler);

        var testRequest = new SetStockPriceRequest()
        {
            NewPrice = 100,
            StockSymbol = "AMZ"
        };

        // Act
        var result = await function.FunctionHandler(testRequest, new Mock<ILambdaContext>().Object);
        
        // Assert
        result.StatusCode.Should().Be(200);

        var response = JsonSerializer.Deserialize<SetStockPriceResponse>(result.Body);
        response?.StockSymbol.Should().Be("AMZ");
        response?.Price.Should().Be(100);
        
        mockEventBus.Verify(p => p.Publish(It.IsAny<StockPriceUpdatedV1Event>()), Times.Once);
        stockRepository.Verify(p => p.UpdateStock(It.IsAny<Stock>()), Times.Once);
    }
    
    [Fact]
    public async Task CanSetStockPrice_When10PercentIncreaseFeatureFlagEnabled_ShouldStoreAndPublishEvent()
    {
        // Arrange
        var stockRepository = new Mock<IStockRepository>();
        stockRepository.Setup(p => p.UpdateStock(It.IsAny<Stock>())).Verifiable();
        var mockEventBus = new Mock<IEventBus>();
        mockEventBus.Setup(p => p.Publish(It.IsAny<Event>())).Verifiable();
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

        var handler = new SetStockPriceHandler(
            stockRepository.Object,
            mockEventBus.Object,
            mockFeatureFlags.Object);
        
        var function = new Function(handler);

        var testRequest = new SetStockPriceRequest()
        {
            NewPrice = 100,
            StockSymbol = "AMZ"
        };

        // Act
        var result = await function.FunctionHandler(testRequest, new Mock<ILambdaContext>().Object);
        
        // Assert
        result.StatusCode.Should().Be(200);

        var response = JsonSerializer.Deserialize<SetStockPriceResponse>(result.Body);
        response?.StockSymbol.Should().Be("AMZ");
        response?.Price.Should().Be(110);
        
        mockEventBus.Verify(p => p.Publish(It.IsAny<StockPriceUpdatedV1Event>()), Times.Once);
        stockRepository.Verify(p => p.UpdateStock(It.IsAny<Stock>()), Times.Once);
    }
    
    [Fact]
    public async Task CanSetStockPrice_WhenReduceStockPriceFeatureFlagEnabled_ShouldStoreAndPublishEvent()
    {
        // Arrange
        var stockRepository = new Mock<IStockRepository>();
        stockRepository.Setup(p => p.UpdateStock(It.IsAny<Stock>())).Verifiable();
        var mockEventBus = new Mock<IEventBus>();
        mockEventBus.Setup(p => p.Publish(It.IsAny<Event>())).Verifiable();
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

        var handler = new SetStockPriceHandler(
            stockRepository.Object,
            mockEventBus.Object,
            mockFeatureFlags.Object);
        
        var function = new Function(handler);

        var testRequest = new SetStockPriceRequest()
        {
            NewPrice = 100,
            StockSymbol = "AMZ"
        };

        // Act
        var result = await function.FunctionHandler(testRequest, new Mock<ILambdaContext>().Object);
        
        // Assert
        result.StatusCode.Should().Be(200);

        var response = JsonSerializer.Deserialize<SetStockPriceResponse>(result.Body);
        response?.StockSymbol.Should().Be("AMZ");
        response?.Price.Should().Be(50);
        
        mockEventBus.Verify(p => p.Publish(It.IsAny<StockPriceUpdatedV1Event>()), Times.Once);
        stockRepository.Verify(p => p.UpdateStock(It.IsAny<Stock>()), Times.Once);
    }
    
    [Fact]
    public async Task CanSetStockPrice_WhenNewStockPriceIsZero_ShouldReturnBadRequest()
    {
        // Arrange
        var stockRepository = new Mock<IStockRepository>();
        stockRepository.Setup(p => p.UpdateStock(It.IsAny<Stock>())).Verifiable();
        var mockEventBus = new Mock<IEventBus>();
        mockEventBus.Setup(p => p.Publish(It.IsAny<Event>())).Verifiable();
        
        var mockFeatureFlags = new Mock<IFeatureFlags>();
        mockFeatureFlags.Setup(
                p => p.Evaluate(
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, object>>(),
                    It.IsAny<object>()))
            .Returns("False");

        var handler = new SetStockPriceHandler(
            stockRepository.Object,
            mockEventBus.Object,
            mockFeatureFlags.Object);
        
        var function = new Function(handler);

        var testRequest = new SetStockPriceRequest()
        {
            NewPrice = 0,
            StockSymbol = "AMZ"
        };

        // Act
        var result = await function.FunctionHandler(testRequest, new Mock<ILambdaContext>().Object);
        
        // Assert
        result.StatusCode.Should().Be(400);
        
        mockEventBus.Verify(p => p.Publish(It.IsAny<StockPriceUpdatedV1Event>()), Times.Never);
        stockRepository.Verify(p => p.UpdateStock(It.IsAny<Stock>()), Times.Never);
    }
}
using System.Text.Json;
using Shared.Events;
using StockTrader.Core.StockAggregate;

namespace Stocks.UnitTests;

public class EventSerializationTests
{
    [Fact]
    public async Task CanSetStockPrice_WhenRequestIsValid_ShouldStoreAndPublishEvent()
    {
        var stockPriceUpdatedEvent = new StockPriceUpdatedEvent("AMZ", 98.70M);

        var eventWrapper = new EventWrapper(stockPriceUpdatedEvent);
        
        var options = new JsonSerializerOptions();
        options.Converters.Add(new EventJsonConverter());

        var eventString = JsonSerializer.Serialize(eventWrapper, options);

        eventString.Should().Contain("\"EventType\":\"StockPriceUpdated\",\"EventVersion\":\"v1\"},\"Data\":{\"stockSymbol\":\"AMZ\",\"newPrice\":98.70}}");
    }
}
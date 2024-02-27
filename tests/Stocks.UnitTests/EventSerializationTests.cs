using System.Text.Json;
using Shared.Events;
using StockTrader.Core.StockAggregate;

namespace Stocks.UnitTests;

public class EventSerializationTests
{
    [Fact]
    public async Task EventSchemaTest_StockPriceUpdated_V1()
    {
        var stockPriceUpdatedEvent = new StockPriceUpdatedEvent("AMZ", 98.70M);

        var eventWrapper = new EventWrapper(stockPriceUpdatedEvent);
        
        var options = new JsonSerializerOptions();
        options.Converters.Add(new EventJsonConverter());

        var eventString = JsonSerializer.Serialize(eventWrapper, options);

        eventString.Should().Contain("\"EventType\":\"StockPriceUpdated\",\"EventVersion\":\"v1\"},\"Data\":{\"StockSymbol\":\"AMZ\",\"NewPrice\":98.70}}");
    }
    
    [Fact]
    public async Task EventSchemaTest_StockPriceUpdated_V2()
    {
        var stockPriceUpdatedEvent = new StockPriceUpdatedEventV2("AMZ", 98.70M, "GBP");

        var eventWrapper = new EventWrapper(stockPriceUpdatedEvent);
        
        var options = new JsonSerializerOptions();
        options.Converters.Add(new EventJsonConverter());

        var eventString = JsonSerializer.Serialize(eventWrapper, options);

        eventString.Should().Contain("\"EventType\":\"StockPriceUpdated\",\"EventVersion\":\"v2\"},\"Data\":{\"StockSymbol\":\"AMZ\",\"NewPrice\":{\"Currency\":\"GBP\",\"Price\":98.70}}}");
    }
}
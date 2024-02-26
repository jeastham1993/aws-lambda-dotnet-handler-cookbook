using System.Text.Json.Serialization;
using SharedKernel.Events;

namespace StockTrader.Core.StockAggregate;

public class StockPriceUpdatedEvent(string stockSymbol, decimal newPrice) : Event
{
    [JsonPropertyName("stockSymbol")]
    public string StockSymbol { get; } = stockSymbol;
    
    [JsonPropertyName("stockSymbol")]
    public decimal NewPrice { get; } = newPrice;
    
    public override string EventType => "StockPriceUpdated";
    public override string EventVersion => "v1";
}
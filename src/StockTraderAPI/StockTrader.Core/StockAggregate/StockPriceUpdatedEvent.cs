using System.Text.Json.Serialization;
using Shared.Events;

namespace StockTrader.Core.StockAggregate;

public class StockPriceUpdatedEvent(string stockSymbol, decimal newPrice) : Event
{
    [JsonPropertyName("StockSymbol")]
    public string StockSymbol { get; } = stockSymbol;
    
    [JsonPropertyName("NewPrice")]
    public decimal NewPrice { get; } = newPrice;
    
    [JsonIgnore]
    public override string EventType => "StockPriceUpdated";
    
    [JsonIgnore]
    public override string EventVersion => "v1";
}
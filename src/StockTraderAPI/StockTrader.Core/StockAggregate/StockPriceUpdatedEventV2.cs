using System.Text.Json.Serialization;
using Shared.Events;

namespace StockTrader.Core.StockAggregate;

public class StockPriceUpdatedEventV2(string stockSymbol, decimal newPrice) : Event
{
    [JsonPropertyName("stock")]
    public string StockSymbol { get; } = stockSymbol;
    
    [JsonPropertyName("price")]
    public decimal NewPrice { get; } = newPrice;
    
    [JsonIgnore]
    public override string EventType => "StockPriceUpdated";
    
    [JsonIgnore]
    public override string EventVersion => "v2";
}
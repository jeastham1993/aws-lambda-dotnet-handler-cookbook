using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Shared.Events;

namespace StockTrader.Core.StockAggregate;

public class StockPriceUpdatedEventV2(string stockSymbol, decimal newPrice, string currency) : Event
{
    [JsonPropertyName("StockSymbol")]
    public string StockSymbol { get; } = stockSymbol;

    [JsonPropertyName("NewPrice")]
    public PriceData PriceData { get; } = new PriceData(currency, newPrice);
    
    [JsonIgnore]
    public override string EventType => "StockPriceUpdated";
    
    [JsonIgnore]
    public override string EventVersion => "v2";
}

public record PriceData(string Currency, decimal Price)
{
    [JsonPropertyName("Currency")]
    public string Currency { get; } = Currency;

    [JsonPropertyName("Price")] public decimal Price { get; } = Price;
}
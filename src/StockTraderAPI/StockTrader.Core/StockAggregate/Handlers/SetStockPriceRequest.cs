namespace StockTrader.Core.StockAggregate.Handlers;

public record SetStockPriceRequest
{
    public string StockSymbol { get; set; } = "";
    
    public string Currency { get; set; } = "";
    
    public decimal NewPrice { get; set; }
}
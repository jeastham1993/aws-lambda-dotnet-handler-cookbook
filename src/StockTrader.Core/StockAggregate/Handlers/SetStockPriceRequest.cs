namespace StockTrader.Core.StockAggregate.Handlers;

public record SetStockPriceRequest
{
    public string StockSymbol { get; set; } = "";
    
    public decimal NewPrice { get; set; }
}
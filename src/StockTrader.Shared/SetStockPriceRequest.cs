namespace StockTrader.Shared;

public record SetStockPriceRequest
{
    public string StockSymbol { get; set; } = "";
    
    public decimal NewPrice { get; set; }
}
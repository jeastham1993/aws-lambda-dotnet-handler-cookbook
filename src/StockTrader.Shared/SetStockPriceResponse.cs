namespace StockTrader.Shared;

public record SetStockPriceResponse
{
    public string StockSymbol { get;set; }

    public decimal Price { get;set; }
}
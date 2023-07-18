namespace StockTrader.Core.StockAggregate.Handlers;

public record SetStockPriceResponse
{
    public string StockSymbol { get; set; } = "";

    public decimal Price { get;set; }
}
namespace StockTrader.Shared;

public record StockSymbol(string Code);

public class Stock
{
    public static Stock CreateStock(StockSymbol symbol)
    {
        return new Stock(symbol);
    }

    internal Stock(StockSymbol symbol)
    {
        this.StockSymbol = symbol;
    }
    
    public StockSymbol StockSymbol { get; private set; }
    
    public decimal CurrentStockPrice { get; private set; }

    public void SetStockPrice(decimal newStockPrice)
    {
        if (newStockPrice <= 0)
        {
            throw new ArgumentException(
                "Stock price must be greater than 0",
                nameof(newStockPrice));
        }

        this.CurrentStockPrice = newStockPrice;
    }
}
namespace StockTrader.Core.StockAggregate;

public record StockSymbol(string Code);

public class Stock
{
    public static Stock CreateStock(StockSymbol symbol)
    {
        return new Stock(symbol);
    }

    private Stock(StockSymbol symbol)
    {
        this.StockSymbol = symbol;
        this.StockHistories = new List<StockHistory>();
    }
    
    public StockSymbol StockSymbol { get; private set; }
    
    public decimal CurrentStockPrice { get; private set; }
    
    public List<StockHistory> StockHistories { get; private set; }

    public void SetStockPrice(decimal newStockPrice)
    {
        if (newStockPrice <= 0)
        {
            throw new ArgumentException(
                "Stock price must be greater than 0",
                nameof(newStockPrice));
        }
        
        this.StockHistories.Add(StockHistory.Create(this.StockSymbol, newStockPrice));
        this.CurrentStockPrice = newStockPrice;
    }
}
namespace StockTrader.Core.StockAggregate;

public class StockHistory
{
    public static StockHistory Create(StockSymbol symbol, decimal price)
    {
        return new StockHistory()
        {
            StockSymbol = symbol,
            OnDate = DateTime.Now,
            Price = price
        };
    }
    
    public StockSymbol StockSymbol { get; private set; }
    
    public decimal Price { get; private set; }
    
    public DateTime OnDate { get; private set; }
}
namespace StockTrader.Core.StockAggregate;

public class StockNotFoundException : Exception
{
    public StockSymbol StockSymbol { get; }

    public StockNotFoundException(StockSymbol stockSymbol) : base()
    {
        StockSymbol = stockSymbol;
    }

    public StockNotFoundException(StockSymbol stockSymbol, string message)
        : base(message)
    {
        StockSymbol = stockSymbol;
    }

    public StockNotFoundException(StockSymbol stockSymbol, string message, Exception inner)
        : base(message, inner)
    {
        StockSymbol = stockSymbol;
    }
    
}
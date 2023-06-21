namespace StockTrader.Shared;

public interface IStockRepository
{
    Task UpdateStock(Stock stock);
    
    Task<Stock> GetStock(StockSymbol symbol);
}
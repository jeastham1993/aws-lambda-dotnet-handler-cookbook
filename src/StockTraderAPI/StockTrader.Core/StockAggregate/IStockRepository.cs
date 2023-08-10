namespace StockTrader.Core.StockAggregate;

public interface IStockRepository
{
    Task UpdateStock(Stock stock);
    
    Task AddHistory(StockHistory history);
    
    Task<StockDTO> GetStock(StockSymbol symbol);
}
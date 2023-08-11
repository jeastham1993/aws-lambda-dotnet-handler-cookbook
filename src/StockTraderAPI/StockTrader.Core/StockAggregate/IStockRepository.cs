namespace StockTrader.Core.StockAggregate;

public interface IStockRepository
{
    Task UpdateStock(Stock stock);
    
    Task<StockDTO> GetCurrentStockPrice(StockSymbol symbol);
    
    Task<StockDTO> GetStockHistory(StockSymbol symbol);
}
namespace StockTrader.Core.StockAggregate;

public interface IStockRepository
{
    Task UpdateStock(Stock stock);
    
    Task<StockDto> GetCurrentStockPrice(StockSymbol symbol);
    
    Task<StockDto> GetStockHistory(StockSymbol symbol);
}
namespace StockTrader.Core.StockAggregate;

public interface IStockRepository
{
    Task UpdateStock(Stock stock);
    
    Task<StockDTO> GetStock(StockSymbol symbol);
}
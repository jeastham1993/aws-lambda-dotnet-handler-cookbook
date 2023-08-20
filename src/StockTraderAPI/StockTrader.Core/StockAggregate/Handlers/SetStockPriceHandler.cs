namespace StockTrader.Core.StockAggregate.Handlers;

using AWS.Lambda.Powertools.Logging;
using AWS.Lambda.Powertools.Tracing;

public class SetStockPriceHandler
{
    private readonly IStockRepository _stockRepository;
    private readonly IStockPriceFeatures _featureFlags;

    public SetStockPriceHandler(IStockRepository stockRepository, IStockPriceFeatures featureFlags)
    {
        this._stockRepository = stockRepository;
        this._featureFlags = featureFlags;
    }
    
    [Tracing]
    public async Task<SetStockPriceResponse> Handle(SetStockPriceRequest request)
    {
        Tracing.AddAnnotation("stock_id", request.StockSymbol);

        if (this._featureFlags.ShouldIncreaseStockPrice())
        {
            Tracing.AddAnnotation("is_price_increase", true);
            
            request.NewPrice *= 1.1M;
        }
        
        if (this._featureFlags.DoesStockCodeHaveDecrease(request.StockSymbol))
        {
            Tracing.AddAnnotation("is_stock_decrease", true);
            
            request.NewPrice *= 0.5M;
        }

        var stock = Stock.CreateStock(new StockSymbol(request.StockSymbol));
        
        stock.SetStockPrice(request.NewPrice);

        await this._stockRepository.UpdateStock(stock);

        return new SetStockPriceResponse() { StockSymbol = stock.StockSymbol.Code, Price = stock.CurrentStockPrice };
    }
}
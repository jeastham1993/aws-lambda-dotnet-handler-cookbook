namespace StockTrader.Core.StockAggregate.Handlers;

using AWS.Lambda.Powertools.Logging;
using AWS.Lambda.Powertools.Tracing;

public class SetStockPriceHandler
{
    private readonly IStockRepository stockRepository;
    private readonly IStockPriceFeatures featureFlags;

    public SetStockPriceHandler(IStockRepository stockRepository, IStockPriceFeatures featureFlags)
    {
        this.stockRepository = stockRepository;
        this.featureFlags = featureFlags;
    }
    
    [Tracing]
    public async Task<SetStockPriceResponse> Handle(SetStockPriceRequest request)
    {
        Tracing.AddAnnotation("stock_id", request.StockSymbol);

        Logger.LogInformation("Handling update stock price request");

        if (this.featureFlags.ShouldIncreaseStockPrice())
        {
            Tracing.AddAnnotation("is_price_increase", true);
            
            request.NewPrice *= 1.1M;
        }
        
        if (this.featureFlags.DoesStockCodeHaveDecrease(request.StockSymbol))
        {
            Tracing.AddAnnotation("is_stock_decrease", true);
            
            request.NewPrice *= 0.5M;
        }

        var stock = Stock.CreateStock(new StockSymbol(request.StockSymbol));
        
        stock.SetStockPrice(request.NewPrice);

        await this.stockRepository.UpdateStock(stock);

        return new SetStockPriceResponse() { StockSymbol = stock.StockSymbol.Code, Price = stock.CurrentStockPrice };
    }
}
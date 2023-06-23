using Shared;

namespace StockTrader.Shared;

using AWS.Lambda.Powertools.Logging;
using AWS.Lambda.Powertools.Tracing;

public class SetStockPriceHandler
{
    private readonly IStockRepository stockRepository;
    private readonly IEventBus eventBus;
    private readonly IFeatureFlags featureFlags;

    public SetStockPriceHandler(IStockRepository stockRepository, IEventBus eventBus, IFeatureFlags featureFlags)
    {
        this.stockRepository = stockRepository;
        this.eventBus = eventBus;
        this.featureFlags = featureFlags;
    }
    
    [Tracing]
    public async Task<SetStockPriceResponse> Handle(SetStockPriceRequest request)
    {
        Tracing.AddAnnotation("stock_id", request.StockSymbol);

        Logger.LogInformation("Handling update stock price request");

        var shouldIncreaseStockPrice = this.featureFlags.Evaluate("ten_percent_share_increase");

        if (shouldIncreaseStockPrice.ToString() == "True")
        {
            Tracing.AddAnnotation("is_price_increase", true);
            
            request.NewPrice *= 1.1M;
        }

        var isCustomerInlineForDecrease = this.featureFlags.Evaluate(
            "reduce_stock_price_for_company",
            new Dictionary<string, object>(1)
            {
                { "stock_symbol", request.StockSymbol }
            });
        
        if (isCustomerInlineForDecrease.ToString() == "True")
        {
            Tracing.AddAnnotation("is_stock_decrease", true);
            
            request.NewPrice *= 0.5M;
        }

        var stock = Stock.CreateStock(new StockSymbol(request.StockSymbol));
        
        stock.SetStockPrice(request.NewPrice);

        await this.stockRepository.UpdateStock(stock);

        await this.eventBus.Publish(
            new StockPriceUpdatedV1Event(
                stock.StockSymbol.Code,
                stock.CurrentStockPrice));

        return new SetStockPriceResponse() { StockSymbol = stock.StockSymbol.Code, Price = stock.CurrentStockPrice };
    }
}
namespace StockTrader.Shared;

using AWS.Lambda.Powertools.Logging;
using AWS.Lambda.Powertools.Tracing;

public class SetStockPriceHandler
{
    private readonly IStockRepository stockRepository;
    private readonly IEventBus eventBus;

    public SetStockPriceHandler(IStockRepository stockRepository, IEventBus eventBus)
    {
        this.stockRepository = stockRepository;
        this.eventBus = eventBus;
    }
    
    [Tracing]
    public async Task<SetStockPriceResponse> Handle(SetStockPriceRequest request)
    {
        Tracing.AddAnnotation("stock_id", request.StockSymbol);

        Logger.LogInformation("Handling update stock price request");

        var stock = Stock.CreateStock(new StockSymbol(request.StockSymbol));
        stock.SetStockPrice(request.NewPrice);

        await this.stockRepository.UpdateStock(stock);

        await this.eventBus.Publish(
            new StockPriceUpdatedV1Event(
                stock.StockSymbol.Code,
                stock.CurrentStockPrice));

        return new SetStockPriceResponse() { StockSymbol = stock.StockSymbol.Code };
    }
}
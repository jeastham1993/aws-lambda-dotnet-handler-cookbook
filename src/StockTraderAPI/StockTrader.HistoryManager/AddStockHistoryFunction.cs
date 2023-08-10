using Amazon.Lambda.Core;
using Amazon.Lambda.Annotations;
using AWS.Lambda.Powertools.Tracing;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace StockTrader.HistoryManager;

using System.Text.Json;

using Amazon.Lambda.SNSEvents;

using SharedKernel.Events;

using StockTrader.Core.StockAggregate;

public class AddStockHistoryFunction
{
    private readonly IStockRepository stockRepository;

    public AddStockHistoryFunction(IStockRepository stockRepository)
    {
        this.stockRepository = stockRepository;
    }
    
    [LambdaFunction]
    [Tracing]
    public async Task UpdateHistory(SNSEvent evt)
    {
        Tracing.AddAnnotation("messages.count", evt.Records.Count);

        foreach (var message in evt.Records)
        {
            var stockPriceUpdatedEvent =
                JsonSerializer.Deserialize<EventWrapper<StockPriceUpdatedEvent>>(message.Sns.Message);
            
            Tracing.AddAnnotation("stock_symbol", stockPriceUpdatedEvent.Data.StockSymbol);

            var isValidPrice = decimal.TryParse(
                stockPriceUpdatedEvent.Data.Price,
                out var parsedPrice);

            if (!isValidPrice)
            {
                throw new Exception("Input event contains an invalid price");
            }

            var stockHistory = StockHistory.Create(
                new StockSymbol(stockPriceUpdatedEvent.Data.StockSymbol),
                parsedPrice);

            await this.stockRepository.AddHistory(stockHistory);
        }
    }
}
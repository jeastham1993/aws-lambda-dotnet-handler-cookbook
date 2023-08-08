using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using AWS.Lambda.Powertools.Tracing;
using Microsoft.Extensions.Options;
using StockTrader.Core.StockAggregate;

namespace StockTrader.Infrastructure;

public class StockRepository : IStockRepository
{
    private readonly InfrastructureSettings _settings;
    private readonly AmazonDynamoDBClient _dynamoDbClient;

    public StockRepository(IOptions<InfrastructureSettings> settings, AmazonDynamoDBClient dynamoDbClient)
    {
        _dynamoDbClient = dynamoDbClient;
        _settings = settings.Value;
    }

    /// <inheritdoc />
    [Tracing]
    public async Task UpdateStock(Stock stock)
    {
        var item = new Dictionary<string, AttributeValue>(3)
        {
            { "StockSymbol", new AttributeValue(stock.StockSymbol.Code) },
            {
                "Price", new AttributeValue
                {
                    N = stock.CurrentStockPrice.ToString()
                }
            }
        };

        if (!string.IsNullOrEmpty(Tracing.GetEntity().TraceId))
            item.Add("TraceIdentifier", new AttributeValue(Tracing.GetEntity().TraceId));
        
        await _dynamoDbClient.PutItemAsync(
            _settings.TableName,
            item);
    }

    [Tracing]
    public async Task<StockDTO> GetStock(StockSymbol symbol)
    {
        var result = await _dynamoDbClient.GetItemAsync(_settings.TableName,
            new Dictionary<string, AttributeValue>(1)
            {
                { "StockSymbol", new AttributeValue(symbol.Code) }
            });

        if (!result.IsItemSet) throw new StockNotFoundException(symbol);

        var stock = new StockDTO(symbol.Code, decimal.Parse(result.Item["Price"].N));

        return stock;
    }
}
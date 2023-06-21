namespace StockTrader.Infrastructure;

using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

using AWS.Lambda.Powertools.Tracing;

using Microsoft.Extensions.Options;

using StockTrader.Shared;

public class StockRepository : IStockRepository
{
    private readonly InfrastructureSettings _settings;
    private readonly AmazonDynamoDBClient _dynamoDbClient;
    
    public StockRepository(IOptions<InfrastructureSettings> settings, AmazonDynamoDBClient dynamoDbClient)
    {
        this._dynamoDbClient = dynamoDbClient;
        this._settings = settings.Value;
    }

    /// <inheritdoc />
    [Tracing]
    public async Task UpdateStock(Stock stock)
    {
        await this._dynamoDbClient.PutItemAsync(
            this._settings.TableName,
            new Dictionary<string, AttributeValue>(2)
            {
                { "StockSymbol", new AttributeValue(stock.StockSymbol.Code) },
                {
                    "Price", new AttributeValue()
                    {
                        N = stock.CurrentStockPrice.ToString()
                    }
                },
            });
    }
}
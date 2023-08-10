namespace StockTrader.Infrastructure;

using System.Text.Json;

using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

using AWS.Lambda.Powertools.Tracing;

using Microsoft.Extensions.Options;

using SharedKernel.Extensions;

using StockTrader.Core.StockAggregate;

public class StockRepository : IStockRepository
{
    private readonly InfrastructureSettings _settings;
    private readonly AmazonDynamoDBClient _dynamoDbClient;

    public StockRepository(IOptions<InfrastructureSettings> settings, AmazonDynamoDBClient dynamoDbClient)
    {
        this._dynamoDbClient = dynamoDbClient;
        this._settings = settings.Value;
    }

    /// <inheritdoc/>
    [Tracing]
    public async Task UpdateStock(Stock stock)
    {
        var item = new Dictionary<string, AttributeValue>(3)
        {
            { "PK", new AttributeValue(stock.StockSymbol.Code) },
            { "SK", new AttributeValue(stock.StockSymbol.Code) },
            { "Type", new AttributeValue("Stock")},
            {
                "Data", new AttributeValue(JsonSerializer.Serialize(stock))
            }
        };

        if (!string.IsNullOrEmpty(Tracing.GetEntity().TraceId))
            item.Add(
                "TraceIdentifier",
                new AttributeValue(Tracing.GetEntity().TraceId));

        await this._dynamoDbClient.PutItemAsync(
            this._settings.TableName,
            item);
    }

    /// <inheritdoc/>
    public async Task AddHistory(StockHistory history)
    {
        await this._dynamoDbClient.PutItemAsync(
            this._settings.TableName,
            new Dictionary<string, AttributeValue>(3)
            {
                { "PK", new AttributeValue(history.StockSymbol.Code) },
                { "SK", new AttributeValue($"HISTORY{history}#{history.OnDate.ToEpochTime()}") },
                { "Type", new AttributeValue("StockHistory")},
                { "Data", new AttributeValue(JsonSerializer.Serialize(history))}
            });
    }

    [Tracing]
    public async Task<StockDTO> GetStock(StockSymbol symbol)
    {
        var result = await this._dynamoDbClient.GetItemAsync(
            this._settings.TableName,
            new Dictionary<string, AttributeValue>(1)
            {
                { "PK", new AttributeValue(symbol.Code) }
            });

        if (!result.IsItemSet) throw new StockNotFoundException(symbol);

        var stock = JsonSerializer.Deserialize<Stock>(result.Item["Data"].S);

        return new StockDTO(stock);
    }
}
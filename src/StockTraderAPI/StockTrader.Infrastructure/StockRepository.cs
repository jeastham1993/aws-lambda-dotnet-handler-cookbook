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
            { "Type", new AttributeValue("Stock") },
            { "StockSymbol", new AttributeValue(stock.StockSymbol.Code) },
            {
                "Price", new AttributeValue
                {
                    N = stock.CurrentStockPrice.ToString()
                }
            },
            {
                "Data", new AttributeValue(JsonSerializer.Serialize(stock))
            }
        };

        var historyItem = new Dictionary<string, AttributeValue>(3)
        {
            { "PK", new AttributeValue(stock.StockSymbol.Code) },
            { "SK", new AttributeValue($"HISTORY#{stock.StockHistories[0].OnDate.ToEpochTime()}") },
            { "Type", new AttributeValue("StockHistory") },
            { "Price", new AttributeValue(){N = stock.StockHistories[0].Price.ToString()} },
            { "OnDate", new AttributeValue(){N = stock.StockHistories[0].OnDate.ToEpochTime().ToString()} }
        };

        if (!string.IsNullOrEmpty(Tracing.GetEntity().TraceId))
            item.Add(
                "TraceIdentifier",
                new AttributeValue(Tracing.GetEntity().TraceId));

        await this._dynamoDbClient.BatchWriteItemAsync(
            new BatchWriteItemRequest(
                new Dictionary<string, List<WriteRequest>>
                {
                    {
                        this._settings.TableName, new List<WriteRequest>(2)
                        {
                            new()
                            {
                                PutRequest = new PutRequest(item)
                            },
                            new()
                            {
                                PutRequest = new PutRequest(historyItem)
                            }
                        }
                    }
                }));

        await this._dynamoDbClient.PutItemAsync(
            this._settings.TableName,
            item);
    }

    [Tracing]
    public async Task<StockDTO> GetCurrentStockPrice(StockSymbol symbol)
    {
        var result = await this._dynamoDbClient.GetItemAsync(
            this._settings.TableName,
            new Dictionary<string, AttributeValue>(1)
            {
                { "PK", new AttributeValue(symbol.Code) },
                { "SK", new AttributeValue(symbol.Code) }
            });

        if (!result.IsItemSet) throw new StockNotFoundException(symbol);

        return new StockDTO(
            result.Item["StockSymbol"].S,
            decimal.Parse(result.Item["Price"].N));
    }

    [Tracing]
    public async Task<StockDTO> GetStockHistory(StockSymbol symbol)
    {
        var result = await this._dynamoDbClient.QueryAsync(
            new QueryRequest(this._settings.TableName)
            {
                KeyConditionExpression = "PK = :pk",
                ExpressionAttributeValues =
                {
                    { ":pk", new AttributeValue(symbol.Code) }
                },
                Limit = 10,
            });

        if (!result.Items.Any()) throw new StockNotFoundException(symbol);

        var stockRecord = result.Items.FirstOrDefault(p => p["Type"].S == "Stock");

        var stockResponse = new StockDTO(
            stockRecord["StockSymbol"].S,
            decimal.Parse(stockRecord["Price"].N));

        foreach (var item in result.Items)
        {
            if (item["Type"].S == "Stock")
                continue;
            
            var dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(long.Parse(item["OnDate"].N));

            stockResponse.History.Add(dateTimeOffset.DateTime, decimal.Parse(item["Price"].N));
        }

        return stockResponse;
    }
}
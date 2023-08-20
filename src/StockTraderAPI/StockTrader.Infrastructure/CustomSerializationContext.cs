namespace StockTrader.Infrastructure;

using System.Text.Json.Serialization;

using Amazon.Lambda.APIGatewayEvents;

using StockTrader.Core.StockAggregate;
using StockTrader.Core.StockAggregate.Handlers;

[JsonSerializable(typeof(SetStockPriceRequest))]
[JsonSerializable(typeof(SetStockPriceResponse))]
[JsonSerializable(typeof(APIGatewayProxyRequest))]
[JsonSerializable(typeof(APIGatewayProxyResponse))]
[JsonSerializable(typeof(StockDto))]
[JsonSerializable(typeof(ApiWrapper<String>))]
[JsonSerializable(typeof(ApiWrapper<StockDto>))]
[JsonSerializable(typeof(ApiWrapper<SetStockPriceResponse>))]
[JsonSerializable(typeof(Dictionary<string, object>))]
public partial class CustomSerializationContext : JsonSerializerContext
{
}
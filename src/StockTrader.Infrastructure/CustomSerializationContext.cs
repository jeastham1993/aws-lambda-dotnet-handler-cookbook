namespace StockTrader.Infrastructure;

using System.Text.Json.Serialization;

using Amazon.Lambda.APIGatewayEvents;

using StockTrader.Core.StockAggregate;
using StockTrader.Core.StockAggregate.Events;
using StockTrader.Core.StockAggregate.Handlers;

[JsonSerializable(typeof(SetStockPriceRequest))]
[JsonSerializable(typeof(SetStockPriceResponse))]
[JsonSerializable(typeof(APIGatewayProxyRequest))]
[JsonSerializable(typeof(APIGatewayProxyResponse))]
[JsonSerializable(typeof(StockDTO))]
[JsonSerializable(typeof(StockPriceUpdatedV1Event))]
[JsonSerializable(typeof(ApiWrapper<String>))]
[JsonSerializable(typeof(ApiWrapper<StockDTO>))]
[JsonSerializable(typeof(ApiWrapper<SetStockPriceResponse>))]
public partial class CustomSerializationContext : JsonSerializerContext
{
}
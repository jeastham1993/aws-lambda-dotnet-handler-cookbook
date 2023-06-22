using System.Text.Json.Serialization;

using Amazon.Lambda.APIGatewayEvents;

using StockTrader.Shared;

[JsonSerializable(typeof(SetStockPriceRequest))]
[JsonSerializable(typeof(SetStockPriceResponse))]
[JsonSerializable(typeof(APIGatewayProxyRequest))]
[JsonSerializable(typeof(APIGatewayProxyResponse))]
[JsonSerializable(typeof(StockPriceUpdatedV1Event))]
public partial class CustomSerializationContext : JsonSerializerContext
{
}
using System.Text.Json.Serialization;
using Amazon.Lambda.APIGatewayEvents;

namespace StockTrader.Shared;

[JsonSerializable(typeof(SetStockPriceRequest))]
[JsonSerializable(typeof(SetStockPriceResponse))]
[JsonSerializable(typeof(APIGatewayProxyRequest))]
[JsonSerializable(typeof(APIGatewayProxyResponse))]
[JsonSerializable(typeof(StockDTO))]
[JsonSerializable(typeof(StockPriceUpdatedV1Event))]
public partial class CustomSerializationContext : JsonSerializerContext
{
}
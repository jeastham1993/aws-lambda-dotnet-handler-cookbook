using System.Text.Json.Serialization;
using Amazon.Lambda.APIGatewayEvents;

namespace AotAspNet;

[JsonSerializable(typeof(APIGatewayProxyRequest))]
[JsonSerializable(typeof(APIGatewayProxyResponse))]
public partial class CustomSerializationContext : JsonSerializerContext
{
    
}
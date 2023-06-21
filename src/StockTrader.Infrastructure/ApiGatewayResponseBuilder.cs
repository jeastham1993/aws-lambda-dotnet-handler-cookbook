namespace StockTrader.Infrastructure;

using System.Net;
using System.Text.Json;

using Amazon.Lambda.APIGatewayEvents;

public static class ApiGatewayResponseBuilder
{
    public static APIGatewayProxyResponse Build<T>(HttpStatusCode statusCode, T body) where T : class
    {
        return new APIGatewayProxyResponse()
        {
            StatusCode = (int)statusCode,
            Body = JsonSerializer.Serialize(body),
            Headers = new Dictionary<string, string>(1)
            {
                {"ContentType", "application/json"},
            }
        };
    }
}
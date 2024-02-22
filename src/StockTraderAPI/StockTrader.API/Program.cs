using Amazon.DynamoDBv2;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using AWS.Lambda.Powertools.Logging;
using Microsoft.Extensions.Options;
using StockTrader.API.Endpoints;
using StockTrader.Infrastructure;

namespace StockTrader.API;

public static class Program
{
    private static GetStockEndpoints _getStockEndpoints;

    public static async Task Main(string[] args)
    {
        var infrastructureSettings = new InfrastructureSettings
        {
            TableName = Environment.GetEnvironmentVariable("TABLE_NAME"),
        };
        
        var dynamoClient = new AmazonDynamoDBClient();
        
        var stockRepository = new StockRepository(Options.Create(infrastructureSettings), dynamoClient);

        _getStockEndpoints = new GetStockEndpoints(stockRepository);
        
        Func<APIGatewayProxyRequest, ILambdaContext, Task<APIGatewayProxyResponse>> handler = FunctionHandler;
        await LambdaBootstrapBuilder.Create(handler, new SourceGeneratorLambdaJsonSerializer<CustomSerializationContext>())
            .Build()
            .RunAsync();
    }

    public static async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        APIGatewayProxyResponse response = null;
        
        switch (request.Resource)
        {
            case "/price/{stockSymbol}":
                switch (request.HttpMethod)
                {
                    case "GET":
                        response = await _getStockEndpoints.GetStockPrice(request.PathParameters["stockSymbol"]);
                        break;
                }

                break;
            case "/history/{stockSymbol}":
                response = await _getStockEndpoints.GetStockHistory(request.PathParameters["stockSymbol"]);
                break;
        }

        return response;
    }
}
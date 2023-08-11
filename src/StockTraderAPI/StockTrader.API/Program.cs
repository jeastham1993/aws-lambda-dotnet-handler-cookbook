using System.Runtime.CompilerServices;
using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using AWS.Lambda.Powertools.Logging;
using AWS.Lambda.Powertools.Parameters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SharedKernel.Features;
using StockTrader.API.Endpoints;
using StockTrader.Core.StockAggregate;
using StockTrader.Core.StockAggregate.Handlers;
using StockTrader.Infrastructure;

namespace StockTrader.API;

public static class Program
{
    private static GetStockPriceEndpoint getStockPriceEndpoint;
    private static SetStockPriceEndpoint setStockPriceEndpoint;

    public static async Task Main(string[] args)
    {
        var config = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();

        var infrastructureSettings = new InfrastructureSettings
        {
            TableName = $"{config["TABLE_NAME"]}{Environment.GetEnvironmentVariable("STACK_POSTFIX")}",
        };
        
        var dynamoClient = new AmazonDynamoDBClient();
        
        var stockRepository = new StockRepository(Options.Create(infrastructureSettings), dynamoClient);

        getStockPriceEndpoint = new GetStockPriceEndpoint(stockRepository);
        setStockPriceEndpoint = new SetStockPriceEndpoint(new SetStockPriceHandler(stockRepository,
            new StockPriceFeatures(
                new FeatureFlags(new Dictionary<string, object>()))));
        
        Func<APIGatewayProxyRequest, ILambdaContext, Task<APIGatewayProxyResponse>> handler = FunctionHandler;
        await LambdaBootstrapBuilder.Create(handler, new DefaultLambdaJsonSerializer())
            .Build()
            .RunAsync();
    }

    public static async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        Logger.LogInformation($"Resource: {request.Resource}");
        
        APIGatewayProxyResponse response = null;
        
        switch (request.Resource)
        {
            case "/price":
                switch (request.HttpMethod)
                {
                    case "POST":
                        response = await setStockPriceEndpoint.SetStockPrice(
                            JsonSerializer.Deserialize<SetStockPriceRequest>(request.Body, CustomSerializationContext.Default.SetStockPriceRequest), context);
                        break;
                }
                break;
            case "/price/{stockSymbol}":
                switch (request.HttpMethod)
                {
                    case "GET":
                        response = await getStockPriceEndpoint.GetStockPrice(request.PathParameters["stockSymbol"]);
                        break;
                }

                break;
            case "/history/{stockSymbol}":
                response = await getStockPriceEndpoint.GetStockHistory(request.PathParameters["stockSymbol"]);
                break;
        }

        return response;
    }
}
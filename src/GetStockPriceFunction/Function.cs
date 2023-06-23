using System.Net;
using Amazon.Lambda.Annotations;
using Amazon.Lambda.Annotations.APIGateway;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;

using AWS.Lambda.Powertools.Idempotency;
using AWS.Lambda.Powertools.Logging;
using AWS.Lambda.Powertools.Metrics;
using AWS.Lambda.Powertools.Tracing;
using StockTrader.Infrastructure;
using StockTrader.Shared;

[assembly: LambdaSerializer(typeof(SourceGeneratorLambdaJsonSerializer<StockTrader.Shared.CustomSerializationContext>))]

namespace GetStockPriceFunction;

public class Function
{
    private readonly IStockRepository _repository;

    public Function(IStockRepository repository)
    {
        this._repository = repository;
    }

    [LambdaFunction]
    [RestApi(LambdaHttpMethod.Get, "/price/{stockSymbol}")]
    [Metrics(CaptureColdStart = true)]
    [Tracing]
    [Idempotent]
    public async Task<APIGatewayProxyResponse> FunctionHandler(string stockSymbol, ILambdaContext context)
    {
        try
        {
            var result = await this._repository.GetStock(new StockSymbol(stockSymbol));

            return ApiGatewayResponseBuilder.Build(
                HttpStatusCode.OK,
                result);
        }
        catch (ArgumentException e)
        {
            Logger.LogError(e);
            
            return ApiGatewayResponseBuilder.Build(
                HttpStatusCode.BadRequest,
                new { });
        }
    }
}

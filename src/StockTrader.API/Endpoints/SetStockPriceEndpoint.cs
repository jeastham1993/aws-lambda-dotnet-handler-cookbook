using System.Net;
using Amazon.Lambda.Annotations;
using Amazon.Lambda.Annotations.APIGateway;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using AWS.Lambda.Powertools.Idempotency;
using AWS.Lambda.Powertools.Logging;
using AWS.Lambda.Powertools.Metrics;
using AWS.Lambda.Powertools.Tracing;
using StockTrader.Core.StockAggregate.Handlers;
using StockTrader.Infrastructure;

namespace StockTrader.API.Endpoints;

public class SetStockPriceEndpoint
{
    private readonly SetStockPriceHandler handler;

    public SetStockPriceEndpoint(SetStockPriceHandler handler)
    {
        this.handler = handler;
    }

    [LambdaFunction]
    [RestApi(LambdaHttpMethod.Put, "/price")]
    [Metrics(CaptureColdStart = true)]
    [Tracing]
    [Idempotent]
    public async Task<APIGatewayProxyResponse> SetStockPrice([FromBody] SetStockPriceRequest request, ILambdaContext context)
    {
        try
        {
            var result = await this.handler.Handle(request);

            return ApiGatewayResponseBuilder.Build(
                HttpStatusCode.OK,
                result);
        }
        catch (ArgumentException e)
        {
            Logger.LogError(e);
            
            return ApiGatewayResponseBuilder.Build(
                HttpStatusCode.BadRequest,
                "");
        }
    }
}
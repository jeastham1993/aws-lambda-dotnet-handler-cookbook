using System.Net;
using Amazon.Lambda.Annotations;
using Amazon.Lambda.Annotations.APIGateway;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using AWS.Lambda.Powertools.Logging;
using AWS.Lambda.Powertools.Tracing;
using Shared.Events;
using StockTrader.Core.StockAggregate;
using StockTrader.Core.StockAggregate.Handlers;
using StockTrader.Infrastructure;

namespace StockTrader.SetStockPriceHandler;

public class Function
{
    private readonly Core.StockAggregate.Handlers.SetStockPriceHandler handler;

    public Function(Core.StockAggregate.Handlers.SetStockPriceHandler handler)
    {
        this.handler = handler;
    }

    [LambdaFunction]
    [RestApi(LambdaHttpMethod.Put, "/price")]
    [Tracing]
    public async Task<APIGatewayProxyResponse> SetStockPrice([FromBody] SetStockPriceRequest request)
    {
        try
        {
            Tracing.AddAnnotation("stock_symbol", request.StockSymbol);
            
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
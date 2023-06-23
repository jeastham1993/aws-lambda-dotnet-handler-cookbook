using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;

[assembly: LambdaSerializer(typeof(SourceGeneratorLambdaJsonSerializer<StockTrader.Shared.CustomSerializationContext>))]

namespace SetStockPriceFunction;

using System.Net;

using Amazon.Lambda.Annotations;
using Amazon.Lambda.Annotations.APIGateway;
using Amazon.Lambda.APIGatewayEvents;

using AWS.Lambda.Powertools.Idempotency;
using AWS.Lambda.Powertools.Logging;
using AWS.Lambda.Powertools.Metrics;
using AWS.Lambda.Powertools.Tracing;
using StockTrader.Infrastructure;
using StockTrader.Shared;

public class Function
{
    private readonly SetStockPriceHandler handler;

    public Function(SetStockPriceHandler handler)
    {
        this.handler = handler;
    }

    [LambdaFunction]
    [RestApi(LambdaHttpMethod.Put, "/price")]
    [Metrics(CaptureColdStart = true)]
    [Tracing]
    [Idempotent]
    public async Task<APIGatewayProxyResponse> FunctionHandler([FromBody] SetStockPriceRequest request, ILambdaContext context)
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

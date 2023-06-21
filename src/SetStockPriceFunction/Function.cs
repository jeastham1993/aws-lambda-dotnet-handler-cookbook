using Amazon.Lambda.Core;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace SetStockPriceFunction;

using System.Net;

using Amazon.Extensions.Configuration.SystemsManager;
using Amazon.Lambda.Annotations;
using Amazon.Lambda.Annotations.APIGateway;
using Amazon.Lambda.APIGatewayEvents;

using AWS.Lambda.Powertools.Idempotency;
using AWS.Lambda.Powertools.Logging;
using AWS.Lambda.Powertools.Metrics;
using AWS.Lambda.Powertools.Tracing;

using Microsoft.Extensions.Configuration;

using StockTrader.Infrastructure;
using StockTrader.Shared;

public class Function
{
    private readonly IConfiguration configuration;
    private readonly SetStockPriceHandler handler;

    public Function(IConfiguration configuration, SetStockPriceHandler handler)
    {
        this.configuration = configuration;
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
            this.configuration.WaitForSystemsManagerReloadToComplete(TimeSpan.FromSeconds(2));
            
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
                new { });
        }
    }
}
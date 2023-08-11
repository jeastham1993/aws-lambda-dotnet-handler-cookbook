using System.Net;
using Amazon.Lambda.Annotations;
using Amazon.Lambda.Annotations.APIGateway;
using Amazon.Lambda.APIGatewayEvents;
using AWS.Lambda.Powertools.Logging;
using AWS.Lambda.Powertools.Tracing;
using StockTrader.Core.StockAggregate;
using StockTrader.Infrastructure;

namespace StockTrader.API.Endpoints;

public class GetStockPriceEndpoint
{
    private readonly IStockRepository repository;

    public GetStockPriceEndpoint(IStockRepository repository)
    {
        this.repository = repository;
    }
    
    [LambdaFunction]
    [RestApi(LambdaHttpMethod.Get, "/price/{stockSymbol}")]
    [Tracing]
    public async Task<APIGatewayProxyResponse> GetStockPrice(string stockSymbol)
    {
        try
        {
            Logger.LogInformation("Entered Handler");
            
            Tracing.AddAnnotation("stock_symbol", stockSymbol);

            var result = await this.repository.GetCurrentStockPrice(new StockSymbol(stockSymbol));
            
            Logger.LogInformation("Retrieving Stock price");

            return ApiGatewayResponseBuilder.Build(
                HttpStatusCode.OK,
                result);
        }
        catch (StockNotFoundException)
        {
            return ApiGatewayResponseBuilder.Build(HttpStatusCode.NotFound, "NotFound");
        }
        catch (ArgumentException e)
        {
            Logger.LogError(e);
            
            return ApiGatewayResponseBuilder.Build(
                HttpStatusCode.BadRequest,
                "Error");
        }
    }
    
    [LambdaFunction]
    [RestApi(LambdaHttpMethod.Get, "/history/{stockSymbol}")]
    [Tracing]
    public async Task<APIGatewayProxyResponse> GetStockHistory(string stockSymbol)
    {
        try
        {
            Tracing.AddAnnotation("stock_symbol", stockSymbol);

            var result = await this.repository.GetStockHistory(new StockSymbol(stockSymbol));

            return ApiGatewayResponseBuilder.Build(
                HttpStatusCode.OK,
                result);
        }
        catch (StockNotFoundException)
        {
            return ApiGatewayResponseBuilder.Build(HttpStatusCode.NotFound, "NotFound");
        }
        catch (ArgumentException e)
        {
            Logger.LogError(e);
            
            return ApiGatewayResponseBuilder.Build(
                HttpStatusCode.BadRequest,
                "Error");
        }
    }
}
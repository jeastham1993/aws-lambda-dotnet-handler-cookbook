﻿namespace SetStockPriceFunction.Endpoints;

using System.Net;

using Amazon.Lambda.Annotations;
using Amazon.Lambda.Annotations.APIGateway;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;

using AWS.Lambda.Powertools.Idempotency;
using AWS.Lambda.Powertools.Logging;
using AWS.Lambda.Powertools.Metrics;
using AWS.Lambda.Powertools.Tracing;

using StockTrader.Core.StockAggregate;
using StockTrader.Infrastructure;

public class GetStockPriceEndpoint
{
    private readonly IStockRepository repository;

    public GetStockPriceEndpoint(IStockRepository repository)
    {
        this.repository = repository;
    }
    
    [LambdaFunction]
    [RestApi(LambdaHttpMethod.Get, "/price/{stockSymbol}")]
    [Metrics(CaptureColdStart = true)]
    [Tracing]
    [Idempotent]
    public async Task<APIGatewayProxyResponse> GetStockPrice(string stockSymbol, ILambdaContext context)
    {
        try
        {
            var result = await this.repository.GetStock(new StockSymbol(stockSymbol));

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
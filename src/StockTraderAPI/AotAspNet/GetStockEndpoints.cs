using System.Net;
using Amazon.Lambda.APIGatewayEvents;
using AWS.Lambda.Powertools.Logging;
using AWS.Lambda.Powertools.Tracing;
using StockTrader.Core.StockAggregate;
using StockTrader.Infrastructure;

namespace AotAspNet;

public class GetStockEndpoints
{
    private readonly IStockRepository repository;

    public GetStockEndpoints(IStockRepository repository)
    {
        this.repository = repository;
    }
    
    [Tracing]
    public async Task<StockDto> GetStockPrice(string stockSymbol)
    {
        try
        {
            Tracing.AddAnnotation("stock_symbol", stockSymbol);

            return await this.repository.GetCurrentStockPrice(new StockSymbol(stockSymbol));
        }
        catch (StockNotFoundException)
        {
            return null;
        }
        catch (ArgumentException e)
        {
            return null;
        }
    }
    
    [Tracing]
    public async Task<StockDto> GetStockHistory(string stockSymbol)
    {
        try
        {
            Tracing.AddAnnotation("stock_symbol", stockSymbol);

            return await this.repository.GetStockHistory(new StockSymbol(stockSymbol));
        }
        catch (StockNotFoundException)
        {
            return null;
        }
        catch (ArgumentException e)
        {
            return null;
        }
    }
    
}
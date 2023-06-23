using Amazon.Lambda.Annotations;
using Microsoft.Extensions.DependencyInjection;
using StockTrader.Infrastructure;
using StockTrader.Shared;

namespace GetStockPriceFunction;

[LambdaStartup]
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSharedServices();
        
        services.AddSingleton<SetStockPriceHandler>();
    }
}

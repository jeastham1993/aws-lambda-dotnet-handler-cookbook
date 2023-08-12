using Amazon.Lambda.Annotations;
using Microsoft.Extensions.DependencyInjection;
using StockTrader.Infrastructure;

namespace StockTrader.SetStockPriceHandler;

[LambdaStartup]
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSharedServices();
    }
}

using Amazon.Lambda.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Shared.Events;
using StockTrader.Infrastructure;

namespace StockTrader.SetStockPriceHandler;

[LambdaStartup]
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSharedServices()
            .AddEventInfrastructure();
    }
}

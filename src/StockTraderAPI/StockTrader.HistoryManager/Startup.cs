using Microsoft.Extensions.DependencyInjection;

namespace StockTrader.HistoryManager;

using StockTrader.Infrastructure;

[Amazon.Lambda.Annotations.LambdaStartup]
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSharedServices();
    }
}
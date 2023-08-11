using Amazon.Lambda.Annotations;
using Microsoft.Extensions.DependencyInjection;
using StockTrader.Infrastructure;

namespace StockTrader.API;

[LambdaStartup]
public class Startup
{
    public static IServiceProvider Services { get; private set; }


    public static void Init()
    {
        var collection = new ServiceCollection();

        collection.AddSharedServices();

        Services = collection.BuildServiceProvider();
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSharedServices();
    }
}

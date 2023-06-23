namespace Stocks.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

using StockTrader.Shared;

public class StockPricingIntegrationTests : IClassFixture<Setup>, IDisposable
{
    private readonly Setup _setup;
    private readonly HttpClient _client;
    private readonly StockApiDriver _driver;
    private bool disposed;

    public StockPricingIntegrationTests(Setup setup)
    {
        _setup = setup;
        _client = new HttpClient()
        {
            BaseAddress = new(setup.ApiUrl),
            DefaultRequestHeaders = { { "INTEGRATION_TEST", "true" } },
        };

        this._driver = new StockApiDriver(_client);
    }
    
    [Fact]
    public async Task SetStockPrice_WithValidInput_ShouldReturnSuccess()
    {
        var testStockSymbol = Guid.NewGuid().ToString();

        var createResponse = await this._driver.CreateStock(
            testStockSymbol,
            100.00M);

        // assert
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        _setup.CreatedStockSymbols.Add(testStockSymbol);
    }
    
    [Fact]
    public async Task ManageStockPrice_WithValidInput_ShouldCreateAndThenRetrieve()
    {
        var testStockSymbol = Guid.NewGuid().ToString();

        var createResponse = await this._driver.CreateStock(
            testStockSymbol,
            100.00M);

        var retrievedStock = await this._driver.GetStock(testStockSymbol);

        retrievedStock.StockSymbol.Should().Be(testStockSymbol);
        retrievedStock.Price.Should().Be(100.00M * 1.1M, "Feature flag is enabled to increase price by 10%");
    }
    
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        _client.Dispose();
    }
}
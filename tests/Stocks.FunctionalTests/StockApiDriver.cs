using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using StockTrader.Core.StockAggregate;
using StockTrader.Core.StockAggregate.Handlers;
using StockTrader.Infrastructure;

namespace Stocks.FunctionalTests;

public class StockApiDriver
{
    private readonly HttpClient httpClient;

    public StockApiDriver(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public async Task<HttpResponseMessage> CreateStock(string stockSymbol, decimal price)
    {
        var request = new SetStockPriceRequest()
        {
            StockSymbol = stockSymbol,
            NewPrice = price
        };
        
        var response = await this.httpClient.PostAsync("price", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json`"));

        return response;
    }

    public async Task<StockDTO?> GetStock(string stockSymbol)
    {
        var response = await this.httpClient.GetAsync($"price/{stockSymbol}");

        if (response.StatusCode != HttpStatusCode.OK)
        {
            return null;
        }

        var stock = await response.Content.ReadFromJsonAsync<ApiWrapper<StockDTO>>();

        return stock.Data;
    }
}
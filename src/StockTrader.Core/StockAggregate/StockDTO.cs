using System.Text.Json.Serialization;

namespace StockTrader.Core.StockAggregate;

public record StockDTO
{
    public StockDTO()
    {
        this.StockSymbol = "";
    }

    public StockDTO(Stock stock)
    {
        this.StockSymbol = stock.StockSymbol.Code;
        this.Price = stock.CurrentStockPrice;
    }

    public StockDTO(string stockSymbol, decimal price)
    {
        this.StockSymbol = stockSymbol;
        this.Price = price;
    }
    
    [JsonPropertyName("stockSymbol")]
    public string StockSymbol { get;set; }
    
    [JsonPropertyName("price")]
    public decimal Price { get; set; }
}
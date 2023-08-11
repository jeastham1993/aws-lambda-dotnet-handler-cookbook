using System.Text.Json.Serialization;

namespace StockTrader.Core.StockAggregate;

public record StockDTO
{
    public StockDTO()
    {
        this.StockSymbol = "";
        this.History = new Dictionary<DateTime, decimal>();
    }

    public StockDTO(Stock stock)
    {
        this.StockSymbol = stock.StockSymbol.Code;
        this.Price = stock.CurrentStockPrice;
        this.History = new Dictionary<DateTime, decimal>();
    }

    public StockDTO(string stockSymbol, decimal price)
    {
        this.StockSymbol = stockSymbol;
        this.Price = price;
        this.History = new Dictionary<DateTime, decimal>();
    }
    
    [JsonPropertyName("stockSymbol")]
    public string StockSymbol { get;set; }
    
    [JsonPropertyName("price")]
    public decimal Price { get; set; }
    
    [JsonPropertyName("history")]
    public Dictionary<DateTime, decimal> History { get; set; }
}
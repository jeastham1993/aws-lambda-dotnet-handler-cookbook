namespace StockTrader.Shared;

public record StockDTO
{
    public StockDTO()
    {
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
    
    public string StockSymbol { get;set; }
    
    public decimal Price { get; set; }
}
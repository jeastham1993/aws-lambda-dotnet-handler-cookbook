using Shared;

namespace StockTrader.Shared;

public class StockPriceUpdatedV1Event : Event
{
    public StockPriceUpdatedV1Event(string stockSymbol, decimal newPrice)
    {
        this.StockSymbol = stockSymbol;
        this.NewPrice = newPrice;
    }
    
    public string StockSymbol { get; set; }
    
    public decimal NewPrice { get; set; }

    /// <inheritdoc />
    public override string EventType => "StockPriceUpdated";

    /// <inheritdoc />
    public override string EventVersion => "v1";
}
namespace StockTrader.HistoryManager;

using SharedKernel.Events;

public class StockPriceUpdatedEvent : Event
{
    public string Price { get; set; }
    
    public string StockSymbol { get; set; }

    /// <inheritdoc />
    public override string EventType => "StockPriceUpdated";

    /// <inheritdoc />
    public override string EventVersion => "V1";
}
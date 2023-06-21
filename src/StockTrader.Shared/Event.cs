namespace StockTrader.Shared;

using System.Text.Json.Serialization;

public abstract class Event
{
    [JsonIgnore]
    public abstract string EventType { get; }

    [JsonIgnore]
    public abstract string EventVersion { get; }
}
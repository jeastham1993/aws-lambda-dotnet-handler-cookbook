using System.Text.Json.Serialization;

namespace Shared;

public abstract class Event
{
    [JsonIgnore]
    public abstract string EventType { get; }

    [JsonIgnore]
    public abstract string EventVersion { get; }
}
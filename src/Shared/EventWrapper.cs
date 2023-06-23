using AWS.Lambda.Powertools.Tracing;

namespace Shared;

public record Metadata(string EventType, string EventVersion)
{
    public string EventId { get; set; } = Guid.NewGuid().ToString();

    public string TraceParent { get; set; } = Tracing.GetEntity().TraceId;

    public string SourceRequestId { get; set; }
    
    public DateTime PublishDate { get; set; } = DateTime.Now;

    public string EventType { get; set; } = EventType;

    public string EventVersion { get; set; } = EventVersion;
}

public class EventWrapper<T> where T : Event
{
    public EventWrapper(T evt)
    {
        this.Data = evt;
        this.Metadata = new Metadata(
            evt.EventType,
            evt.EventVersion);
    }
    public Metadata Metadata { get; set; }
    
    public T Data { get; set; }
}
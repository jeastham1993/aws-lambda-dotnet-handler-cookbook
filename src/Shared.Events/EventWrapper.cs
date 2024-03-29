﻿using System.Text.Json.Serialization;
using AWS.Lambda.Powertools.Tracing;

namespace Shared.Events;

public record Metadata
{
    [JsonConstructor]
    public Metadata()
    {
    }

    public Metadata(string eventType, string eventVersion)
    {
        this.EventType = eventType;
        this.EventVersion = eventVersion;
    }
    
    public string EventId { get; set; } = Guid.NewGuid().ToString();

    public string TraceParent { get; set; } = Tracing.GetEntity().TraceId;

    public string SourceRequestId { get; set; }
    
    public DateTime PublishDate { get; set; } = DateTime.Now;

    public string EventType { get; set; }

    public string EventVersion { get; set; }
}

public class EventWrapper
{
    [JsonConstructor]
    public EventWrapper()
    {
    }
    
    public EventWrapper(Event evt)
    {
        this.Data = evt;
        this.Metadata = new Metadata(
            evt.EventType,
            evt.EventVersion);
    }
    public Metadata Metadata { get; set; }
    
    public Event Data { get; set; }
}
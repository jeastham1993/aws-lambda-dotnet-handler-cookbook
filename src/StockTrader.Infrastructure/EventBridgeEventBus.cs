﻿namespace StockTrader.Infrastructure;

using System.Text.Json;

using Amazon.EventBridge;
using Amazon.EventBridge.Model;

using AWS.Lambda.Powertools.Tracing;

using Microsoft.Extensions.Options;

using StockTrader.Shared;

public class EventBridgeEventBus : IEventBus
{
    private readonly AmazonEventBridgeClient _eventBridgeClient;
    private readonly InfrastructureSettings _settings;

    public EventBridgeEventBus(IOptions<InfrastructureSettings> settings, AmazonEventBridgeClient eventBridgeClient)
    {
        this._eventBridgeClient = eventBridgeClient;
        this._settings = settings.Value;
    }

    /// <inheritdoc />
    [Tracing]
    public async Task Publish<T>(T evt)
        where T : Event
    {
        await this._eventBridgeClient.PutEventsAsync(
            new PutEventsRequest()
            {
                Entries = new List<PutEventsRequestEntry>(1)
                {
                     new PutEventsRequestEntry()
                     {
                         EventBusName = this._settings.EventBusName,
                         Detail = JsonSerializer.Serialize(evt, typeof(T), CustomSerializationContext.Default),
                         DetailType = evt.EventType,
                         Source = this._settings.ServiceName,
                         TraceHeader = Tracing.GetEntity().TraceId
                     }
                }
            });
    }
}
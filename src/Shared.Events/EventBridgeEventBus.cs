namespace SharedKernel.Events;

using System.Text.Json;

using Amazon.EventBridge;
using Amazon.EventBridge.Model;

using AWS.Lambda.Powertools.Tracing;

using Microsoft.Extensions.Options;

using Shared.Events;

public class EventBridgeEventBus : IEventBus
{
    private readonly AmazonEventBridgeClient _eventBridgeClient;
    private readonly SharedSettings _settings;

    public EventBridgeEventBus(IOptions<SharedSettings> settings, AmazonEventBridgeClient eventBridgeClient)
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
                         Detail = JsonSerializer.Serialize(evt),
                         DetailType = evt.EventType,
                         Source = this._settings.ServiceName,
                         TraceHeader = Tracing.GetEntity().TraceId
                     }
                }
            });
    }
}
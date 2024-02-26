using AWS.Lambda.Powertools.Logging;

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
    public async Task Publish(Event evt)
    {
        var wrapper = new EventWrapper(evt);
        
        var evtData = JsonSerializer.Serialize(wrapper);

        Logger.LogInformation(evtData);
        
        await this._eventBridgeClient.PutEventsAsync(
            new PutEventsRequest()
            {
                Entries = new List<PutEventsRequestEntry>(1)
                {
                     new PutEventsRequestEntry()
                     {
                         EventBusName = this._settings.EventBusName,
                         Detail = evtData,
                         DetailType = evt.EventType,
                         Source = this._settings.ServiceName,
                         TraceHeader = Tracing.GetEntity().TraceId
                     }
                }
            });
    }
}
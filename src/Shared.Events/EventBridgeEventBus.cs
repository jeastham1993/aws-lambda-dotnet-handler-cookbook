using System.Text.Json;
using Amazon.EventBridge;
using Amazon.EventBridge.Model;
using AWS.Lambda.Powertools.Logging;
using AWS.Lambda.Powertools.Tracing;
using Microsoft.Extensions.Options;

namespace Shared.Events;

public class EventBridgeEventBus : IEventBus
{
    private readonly AmazonEventBridgeClient _eventBridgeClient;
    private readonly SharedSettings _settings;
    private readonly JsonSerializerOptions _options;

    public EventBridgeEventBus(IOptions<SharedSettings> settings, AmazonEventBridgeClient eventBridgeClient)
    {
        this._eventBridgeClient = eventBridgeClient;
        this._settings = settings.Value;
        this._options = new JsonSerializerOptions();
        this._options.Converters.Add(new EventJsonConverter());
    }

    /// <inheritdoc />
    [Tracing]
    public async Task Publish(Event evt)
    {
        var wrapper = new EventWrapper(evt);
        
        var evtData = JsonSerializer.Serialize(wrapper, this._options);

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

    public async Task Publish(List<Event> evts)
    {
        var entries = new List<PutEventsRequestEntry>(evts.Count);

        foreach (var evt in evts)
        {
            var wrapper = new EventWrapper(evt);

            var evtData = JsonSerializer.Serialize(wrapper, this._options);

            Logger.LogInformation(evtData);

            entries.Add(new PutEventsRequestEntry()
            {
                EventBusName = this._settings.EventBusName,
                Detail = evtData,
                DetailType = evt.EventType,
                Source = this._settings.ServiceName,
                TraceHeader = Tracing.GetEntity().TraceId
            });
        }

        await this._eventBridgeClient.PutEventsAsync(
            new PutEventsRequest()
            {
                Entries = entries
            });
    }
}
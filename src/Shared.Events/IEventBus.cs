namespace Shared.Events;

public interface IEventBus
{
    Task Publish(Event evt);
    Task Publish(List<Event> evts);
}
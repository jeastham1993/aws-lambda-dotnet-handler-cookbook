namespace Shared;

public interface IEventBus
{
    Task Publish<T>(T evt)
        where T : Event;
}
using SharedKernel.Events;

namespace Shared.Events;

public interface IEventBus
{
    Task Publish(Event evt);
}
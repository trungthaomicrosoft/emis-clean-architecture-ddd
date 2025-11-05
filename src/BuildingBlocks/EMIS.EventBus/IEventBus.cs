namespace EMIS.EventBus;

/// <summary>
/// Event bus interface for publishing and subscribing to integration events.
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// Publishes an integration event to the event bus.
    /// </summary>
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IntegrationEvent;
}

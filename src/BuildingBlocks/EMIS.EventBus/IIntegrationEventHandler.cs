namespace EMIS.EventBus;

/// <summary>
/// Interface for integration event handlers.
/// </summary>
/// <typeparam name="TEvent">The type of integration event to handle</typeparam>
public interface IIntegrationEventHandler<in TEvent>
    where TEvent : IntegrationEvent
{
    /// <summary>
    /// Handles the integration event.
    /// </summary>
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
}

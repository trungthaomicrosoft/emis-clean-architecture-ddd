namespace EMIS.EventBus;

/// <summary>
/// Integration event handler interface
/// </summary>
public interface IIntegrationEventHandler<in TEvent> where TEvent : IntegrationEvent
{
    Task Handle(TEvent @event, CancellationToken cancellationToken);
}

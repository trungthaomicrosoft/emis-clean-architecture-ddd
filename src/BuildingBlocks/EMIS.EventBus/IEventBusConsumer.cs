namespace EMIS.EventBus;

/// <summary>
/// Abstraction for event bus consumer.
/// Allows subscribing to events and registering handlers without coupling to specific message broker implementation.
/// </summary>
public interface IEventBusConsumer
{
    /// <summary>
    /// Subscribe to an event type with a specific handler.
    /// The consumer will automatically route messages to the registered handler.
    /// </summary>
    /// <typeparam name="TEvent">The integration event type to subscribe to</typeparam>
    /// <typeparam name="THandler">The handler that will process the event</typeparam>
    void Subscribe<TEvent, THandler>()
        where TEvent : IntegrationEvent
        where THandler : IIntegrationEventHandler<TEvent>;
}

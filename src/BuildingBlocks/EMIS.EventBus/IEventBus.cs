namespace EMIS.EventBus;

/// <summary>
/// Event bus interface for publishing and subscribing to integration events.
/// Abstraction that works with any message broker (Kafka, RabbitMQ, Azure Service Bus, etc.)
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// Publishes an integration event to the event bus.
    /// If the event implements IOrderedEvent, the underlying message broker
    /// will ensure ordered processing for events with the same ordering key.
    /// </summary>
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IntegrationEvent;

    /// <summary>
    /// Publishes an integration event with an explicit ordering key.
    /// Events with the same ordering key will be processed sequentially by the message broker.
    /// 
    /// Implementation notes:
    /// - Kafka: Uses as partition key
    /// - RabbitMQ: Uses as routing key with consistent hashing
    /// - Azure Service Bus: Uses as session ID
    /// - In-memory: Groups into sequential processing queue
    /// </summary>
    /// <param name="event">The event to publish</param>
    /// <param name="orderingKey">Key for ordered processing (e.g., ConversationId, StudentId)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task PublishAsync<TEvent>(TEvent @event, string orderingKey, CancellationToken cancellationToken = default)
        where TEvent : IntegrationEvent;
}

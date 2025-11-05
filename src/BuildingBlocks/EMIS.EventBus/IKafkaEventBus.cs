namespace EMIS.EventBus;

/// <summary>
/// Kafka Event Bus implementation for publishing integration events
/// </summary>
public interface IKafkaEventBus
{
    /// <summary>
    /// Publish an integration event to Kafka topic
    /// </summary>
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) 
        where TEvent : IIntegrationEvent;
}

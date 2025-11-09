namespace EMIS.EventBus;

/// <summary>
/// Interface for events that require ordered processing based on a logical grouping key.
/// Events with the same ordering key should be processed in order.
/// 
/// This is a message broker agnostic abstraction:
/// - Kafka: Maps to partition key
/// - RabbitMQ: Maps to routing key with consistent hashing
/// - Azure Service Bus: Maps to session ID
/// - In-memory: Maps to sequential processing queue
/// </summary>
/// <remarks>
/// Use cases:
/// - Chat messages in the same conversation should maintain order
/// - Student updates should be processed sequentially
/// - Financial transactions for the same account need ordering
/// </remarks>
public interface IOrderedEvent : IIntegrationEvent
{
    /// <summary>
    /// Returns the logical grouping key for ordered processing.
    /// Events with the same ordering key will be processed sequentially.
    /// </summary>
    /// <returns>Ordering key (e.g., ConversationId, StudentId, AccountId)</returns>
    string GetOrderingKey();
}

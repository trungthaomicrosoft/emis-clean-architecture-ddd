namespace EMIS.EventBus;

/// <summary>
/// Kafka-specific Event Bus interface.
/// Extends IEventBus with no additional methods - kept for backward compatibility
/// and explicit Kafka registration in DI container.
/// </summary>
/// <remarks>
/// In most cases, you should inject IEventBus instead of IKafkaEventBus
/// to keep your code message-broker agnostic.
/// </remarks>
public interface IKafkaEventBus : IEventBus
{
    // No additional methods - IEventBus already has everything needed
    // This interface exists for:
    // 1. Backward compatibility
    // 2. Explicit Kafka service registration
    // 3. Future Kafka-specific features if needed
}

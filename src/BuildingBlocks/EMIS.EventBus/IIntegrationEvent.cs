namespace EMIS.EventBus;

/// <summary>
/// Base interface for integration events.
/// Integration events are used for communication between microservices.
/// </summary>
public interface IIntegrationEvent
{
    /// <summary>
    /// Unique identifier for the event.
    /// </summary>
    string EventId { get; }

    /// <summary>
    /// When the event was created.
    /// </summary>
    DateTime CreationDate { get; }
}

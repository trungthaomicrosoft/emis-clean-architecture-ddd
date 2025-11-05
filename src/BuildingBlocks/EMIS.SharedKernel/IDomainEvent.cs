namespace EMIS.SharedKernel;

/// <summary>
/// Base interface for all domain events.
/// Domain events are used to notify other parts of the system about changes in the domain.
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// Unique identifier for the event.
    /// </summary>
    string EventId { get; }

    /// <summary>
    /// When the event occurred.
    /// </summary>
    DateTime OccurredOn { get; }
}

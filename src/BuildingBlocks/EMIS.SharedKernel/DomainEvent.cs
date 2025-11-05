namespace EMIS.SharedKernel;

/// <summary>
/// Base class for domain events with common properties.
/// </summary>
public abstract class DomainEvent : IDomainEvent
{
    protected DomainEvent()
    {
        EventId = Guid.NewGuid().ToString();
        OccurredOn = DateTime.UtcNow;
    }

    public string EventId { get; private set; }
    public DateTime OccurredOn { get; private set; }
}

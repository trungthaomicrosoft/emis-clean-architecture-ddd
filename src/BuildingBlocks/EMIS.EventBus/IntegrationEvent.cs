namespace EMIS.EventBus;

/// <summary>
/// Base class for integration events
/// </summary>
public abstract class IntegrationEvent : IIntegrationEvent
{
    public Guid Id { get; set; }
    public DateTime OccurredOn { get; set; }

    protected IntegrationEvent()
    {
        Id = Guid.NewGuid();
        OccurredOn = DateTime.UtcNow;
    }
}

namespace EMIS.EventBus;

/// <summary>
/// Base class for integration events.
/// </summary>
public abstract class IntegrationEvent : IIntegrationEvent
{
    protected IntegrationEvent()
    {
        EventId = Guid.NewGuid().ToString();
        CreationDate = DateTime.UtcNow;
    }

    protected IntegrationEvent(string eventId, DateTime createDate)
    {
        EventId = eventId;
        CreationDate = createDate;
    }

    public string EventId { get; private set; }
    public DateTime CreationDate { get; private set; }
}

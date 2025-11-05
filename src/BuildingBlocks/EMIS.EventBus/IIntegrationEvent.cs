namespace EMIS.EventBus;

/// <summary>
/// Base interface for all integration events
/// </summary>
public interface IIntegrationEvent
{
    Guid Id { get; set; }
    DateTime OccurredOn { get; set; }
}

using EMIS.EventBus;
using EMIS.EventBus.IntegrationEvents;
using EMIS.SharedKernel;
using Identity.Domain.Events;
using Microsoft.Extensions.Logging;

namespace Identity.Application.EventHandlers;

/// <summary>
/// Domain Event Handler: Listen to TenantCreatedEvent (domain) 
/// and publish TenantCreatedIntegrationEvent (cross-service)
/// </summary>
public class TenantCreatedDomainEventHandler : IDomainEventHandler<TenantCreatedEvent>
{
    private readonly IEventBus _eventBus;
    private readonly ILogger<TenantCreatedDomainEventHandler> _logger;

    public TenantCreatedDomainEventHandler(
        IEventBus eventBus,
        ILogger<TenantCreatedDomainEventHandler> logger)
    {
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task Handle(TenantCreatedEvent domainEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Handling TenantCreatedEvent for Tenant {TenantId}, Subdomain: {Subdomain}",
            domainEvent.TenantId, domainEvent.Subdomain);

        try
        {
            // Convert domain event to integration event
            var integrationEvent = new TenantCreatedIntegrationEvent(
                domainEvent.TenantId,
                domainEvent.TenantName,
                domainEvent.Subdomain,
                domainEvent.SchoolAdminId,
                domainEvent.SubscriptionPlan,
                domainEvent.SubscriptionExpiresAt,
                maxUsers: GetMaxUsersByPlan(domainEvent.SubscriptionPlan),
                connectionString: null); // TODO: Generate connection string if needed

            // Publish to Kafka for other services
            await _eventBus.PublishAsync(integrationEvent, cancellationToken);

            _logger.LogInformation(
                "Published TenantCreatedIntegrationEvent for Tenant {TenantId} to Kafka",
                domainEvent.TenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error publishing TenantCreatedIntegrationEvent for Tenant {TenantId}",
                domainEvent.TenantId);
            throw;
        }
    }

    private static int GetMaxUsersByPlan(string subscriptionPlan)
    {
        return subscriptionPlan switch
        {
            "Trial" => 50,
            "Basic" => 100,
            "Standard" => 500,
            "Professional" => 2000,
            "Enterprise" => int.MaxValue,
            _ => 50
        };
    }
}

/// <summary>
/// Interface for domain event handlers (similar to MediatR's INotificationHandler)
/// </summary>
public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task Handle(TEvent domainEvent, CancellationToken cancellationToken);
}

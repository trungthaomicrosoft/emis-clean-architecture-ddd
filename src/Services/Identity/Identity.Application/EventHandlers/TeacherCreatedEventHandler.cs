using EMIS.EventBus;
using EMIS.EventBus.IntegrationEvents;
using Identity.Application.Commands;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Identity.Application.EventHandlers;

/// <summary>
/// Handle TeacherCreatedIntegrationEvent by creating a User account
/// </summary>
public class TeacherCreatedEventHandler : IIntegrationEventHandler<TeacherCreatedIntegrationEvent>
{
    private readonly IMediator _mediator;
    private readonly ILogger<TeacherCreatedEventHandler> _logger;

    public TeacherCreatedEventHandler(
        IMediator mediator,
        ILogger<TeacherCreatedEventHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(TeacherCreatedIntegrationEvent @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Handling TeacherCreatedIntegrationEvent for Teacher {TeacherId}, Phone {PhoneNumber}",
            @event.TeacherId, @event.PhoneNumber);

        try
        {
            // TODO: Inject ITenantContext to get current tenant - for now use event's TenantId
            // In real implementation, you'd set tenant context before calling the handler
            
            // Create user with PendingActivation status (no password yet)
            var command = new CreateUserCommand
            {
                PhoneNumber = @event.PhoneNumber,
                FullName = @event.FullName,
                Email = @event.Email,
                Role = Identity.Domain.Enums.UserRole.Teacher,
                EntityId = @event.TeacherId // Link to Teacher entity
            };

            var result = await _mediator.Send(command, cancellationToken);

            if (result.Success)
            {
                _logger.LogInformation(
                    "Successfully created User account for Teacher {TeacherId}. UserId: {UserId}",
                    @event.TeacherId, result.Data);
            }
            else
            {
                _logger.LogWarning(
                    "Failed to create User account for Teacher {TeacherId}. Error: {ErrorCode} - {ErrorMessage}",
                    @event.TeacherId, result.Error?.Code, result.Error?.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error handling TeacherCreatedIntegrationEvent for Teacher {TeacherId}",
                @event.TeacherId);
            throw; // Will trigger Kafka retry
        }
    }
}

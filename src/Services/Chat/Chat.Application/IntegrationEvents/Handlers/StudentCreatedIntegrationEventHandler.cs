using Chat.Application.Commands.Conversations;
using EMIS.EventBus;
using EMIS.EventBus.IntegrationEvents;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Chat.Application.IntegrationEvents.Handlers;

/// <summary>
/// Handler for StudentCreatedIntegrationEvent
/// Automatically creates a student group conversation when a new student is created
/// </summary>
public class StudentCreatedIntegrationEventHandler 
    : IIntegrationEventHandler<StudentCreatedIntegrationEvent>
{
    private readonly IMediator _mediator;
    private readonly ILogger<StudentCreatedIntegrationEventHandler> _logger;

    public StudentCreatedIntegrationEventHandler(
        IMediator mediator,
        ILogger<StudentCreatedIntegrationEventHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(StudentCreatedIntegrationEvent @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Received StudentCreatedIntegrationEvent for student {StudentId} ({StudentName}) in class {ClassId}",
            @event.StudentId, @event.StudentName, @event.ClassId);

        try
        {
            // Validate that student has at least one parent
            if (@event.Parents == null || !@event.Parents.Any())
            {
                _logger.LogWarning(
                    "Student {StudentId} has no parents. Skipping student group creation.",
                    @event.StudentId);
                return;
            }

            // Create command to create student group
            var command = new CreateStudentGroupFromEventCommand
            {
                TenantId = @event.TenantId,
                StudentId = @event.StudentId,
                ClassId = @event.ClassId,
                StudentName = @event.StudentName,
                Parents = @event.Parents.Select(p => (p.ParentId, p.ParentName)).ToList(),
                CreatedBy = @event.CreatedBy
            };

            // Send command via MediatR
            var result = await _mediator.Send(command, cancellationToken);

            if (result.Success)
            {
                _logger.LogInformation(
                    "Successfully created student group conversation for student {StudentId}",
                    @event.StudentId);
            }
            else
            {
                _logger.LogError(
                    "Failed to create student group for student {StudentId}: {ErrorMessage}",
                    @event.StudentId, result.Error?.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error handling StudentCreatedIntegrationEvent for student {StudentId}",
                @event.StudentId);
            
            // Don't throw - we don't want to retry indefinitely
            // Consider implementing a dead letter queue for failed events
        }
    }
}

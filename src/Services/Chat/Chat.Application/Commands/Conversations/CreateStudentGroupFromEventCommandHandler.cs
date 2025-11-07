using AutoMapper;
using Chat.Application.DTOs;
using Chat.Application.Interfaces;
using Chat.Domain.Aggregates;
using Chat.Domain.Enums;
using Chat.Domain.Repositories;
using EMIS.BuildingBlocks.ApiResponse;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Chat.Application.Commands.Conversations;

/// <summary>
/// Handler for creating student group from integration event
/// This handler has parent info from the event, only needs to fetch teachers
/// </summary>
public class CreateStudentGroupFromEventCommandHandler
    : IRequestHandler<CreateStudentGroupFromEventCommand, ApiResponse<ConversationDto>>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly ITeacherIntegrationService _teacherService;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateStudentGroupFromEventCommandHandler> _logger;

    public CreateStudentGroupFromEventCommandHandler(
        IConversationRepository conversationRepository,
        ITeacherIntegrationService teacherService,
        IMapper mapper,
        ILogger<CreateStudentGroupFromEventCommandHandler> logger)
    {
        _conversationRepository = conversationRepository;
        _teacherService = teacherService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<ConversationDto>> Handle(
        CreateStudentGroupFromEventCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            // 1. Check if student group already exists
            var existingGroup = await _conversationRepository.FindStudentGroupByStudentIdAsync(
                request.TenantId,
                request.StudentId,
                cancellationToken);

            if (existingGroup != null)
            {
                _logger.LogWarning(
                    "Student group already exists for student {StudentId}. Skipping creation.",
                    request.StudentId);
                
                // Return existing group instead of error
                var existingDto = _mapper.Map<ConversationDto>(existingGroup);
                return ApiResponse<ConversationDto>.SuccessResult(existingDto);
            }

            // 2. Validate parents (already provided by event)
            if (!request.Parents.Any())
            {
                _logger.LogWarning(
                    "Student {StudentId} has no parents in the event data",
                    request.StudentId);
                return ApiResponse<ConversationDto>.ErrorResult(
                    "Student must have at least one parent to create a group conversation",
                    400);
            }

            // 3. Fetch teachers from Teacher Service
            _logger.LogInformation(
                "Fetching teachers for class {ClassId} from Teacher Service",
                request.ClassId);

            var teachers = await _teacherService.GetTeachersByClassIdAsync(
                request.TenantId,
                request.ClassId,
                cancellationToken);

            if (!teachers.Any())
            {
                _logger.LogWarning(
                    "No teachers found for class {ClassId}. Will create group without teachers for now.",
                    request.ClassId);
                
                // Note: We could either:
                // A) Skip group creation if no teachers (strict)
                // B) Create group with only parents, add teachers later (flexible)
                // Choosing B for better UX
            }

            // 4. Generate group name
            var groupName = $"Group: {request.StudentName}";

            _logger.LogInformation(
                "Creating student group '{GroupName}' for student {StudentName} with {ParentCount} parents and {TeacherCount} teachers",
                groupName, request.StudentName, request.Parents.Count, teachers.Count);

            // 5. Create conversation using factory method
            Conversation conversation;

            if (teachers.Any())
            {
                // Get primary teacher (prefer head teacher, otherwise first teacher)
                var primaryTeacher = teachers.FirstOrDefault(t => t.IsHeadTeacher) ?? teachers.First();

                conversation = Conversation.CreateStudentGroup(
                    request.TenantId,
                    request.StudentId,
                    request.StudentName,
                    primaryTeacher.Id,
                    primaryTeacher.Name,
                    request.Parents);

                // 6. Add additional teachers as admins
                foreach (var teacher in teachers.Where(t => t.Id != primaryTeacher.Id))
                {
                    conversation.AddParticipant(
                        teacher.Id,
                        teacher.Name,
                        ParticipantRole.Admin);
                }
            }
            else
            {
                // Create group with only parents for now
                // Use first parent as temporary "creator" for the factory method
                var firstParent = request.Parents.First();
                
                conversation = Conversation.CreateStudentGroup(
                    request.TenantId,
                    request.StudentId,
                    request.StudentName,
                    firstParent.ParentId, // Temporary - will be replaced when teacher assigned
                    firstParent.ParentName,
                    request.Parents.Skip(1).ToList());
                
                _logger.LogWarning(
                    "Created student group {ConversationId} without teachers. Teachers should be added when assigned to class.",
                    conversation.Id);
            }

            // 7. Save conversation
            await _conversationRepository.AddAsync(conversation, cancellationToken);

            _logger.LogInformation(
                "Successfully created student group conversation {ConversationId} for student {StudentId} via event",
                conversation.Id, request.StudentId);

            // 8. Map to DTO and return
            var conversationDto = _mapper.Map<ConversationDto>(conversation);
            return ApiResponse<ConversationDto>.SuccessResult(conversationDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to create student group from event for student {StudentId}: {ErrorMessage}",
                request.StudentId, ex.Message);

            return ApiResponse<ConversationDto>.ErrorResult(
                $"Failed to create student group: {ex.Message}",
                500);
        }
    }
}

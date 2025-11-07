using AutoMapper;
using Chat.Application.DTOs;
using Chat.Application.Interfaces;
using Chat.Domain.Aggregates;
using Chat.Domain.Enums;
using Chat.Domain.Repositories;
using Chat.Domain.ValueObjects;
using EMIS.BuildingBlocks.ApiResponse;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Chat.Application.Commands.Conversations;

/// <summary>
/// Handler for creating student group conversations
/// </summary>
public class CreateStudentGroupCommandHandler 
    : IRequestHandler<CreateStudentGroupCommand, ApiResponse<ConversationDto>>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IStudentIntegrationService _studentService;
    private readonly ITeacherIntegrationService _teacherService;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateStudentGroupCommandHandler> _logger;

    public CreateStudentGroupCommandHandler(
        IConversationRepository conversationRepository,
        IStudentIntegrationService studentService,
        ITeacherIntegrationService teacherService,
        IMapper mapper,
        ILogger<CreateStudentGroupCommandHandler> logger)
    {
        _conversationRepository = conversationRepository;
        _studentService = studentService;
        _teacherService = teacherService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<ConversationDto>> Handle(
        CreateStudentGroupCommand request,
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
                return ApiResponse<ConversationDto>.ErrorResult(
                    "Student group already exists for this student", 
                    409);

            // 2. Fetch student information including parents from Student Service
            _logger.LogInformation(
                "Fetching student {StudentId} information from Student Service", 
                request.StudentId);
            
            var studentInfo = await _studentService.GetStudentWithParentsAsync(
                request.TenantId,
                request.StudentId,
                cancellationToken);

            if (studentInfo == null)
            {
                _logger.LogWarning(
                    "Student {StudentId} not found in Student Service", 
                    request.StudentId);
                return ApiResponse<ConversationDto>.ErrorResult(
                    "Student not found", 
                    404);
            }

            // 3. Validate that student has at least one parent
            if (!studentInfo.Parents.Any())
            {
                _logger.LogWarning(
                    "Student {StudentId} has no parents registered", 
                    request.StudentId);
                return ApiResponse<ConversationDto>.ErrorResult(
                    "Student must have at least one parent to create a group conversation", 
                    400);
            }

            // 4. Fetch teachers from Teacher Service
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
                    "No teachers found for class {ClassId}",
                    request.ClassId);
                return ApiResponse<ConversationDto>.ErrorResult(
                    "No teachers assigned to this class",
                    400);
            }
            
            // 5. Prepare participant data
            var parents = studentInfo.Parents
                .Select(p => (p.Id, p.Name))
                .ToList();

            // Get primary teacher (prefer head teacher, otherwise first teacher)
            var primaryTeacher = teachers.FirstOrDefault(t => t.IsHeadTeacher) 
                ?? teachers.First();

            _logger.LogInformation(
                "Creating student group for student {StudentName} with {ParentCount} parents and {TeacherCount} teachers",
                studentInfo.Name, parents.Count, teachers.Count);

            // 6. Create conversation using factory method
            var conversation = Conversation.CreateStudentGroup(
                request.TenantId,
                request.StudentId,
                studentInfo.Name,
                primaryTeacher.Id,
                primaryTeacher.Name,
                parents);

            // 7. Add additional teachers as admins
            foreach (var teacher in teachers.Where(t => t.Id != primaryTeacher.Id))
            {
                conversation.AddParticipant(
                    teacher.Id, 
                    teacher.Name, 
                    ParticipantRole.Admin);
            }

            // 8. Save conversation
            await _conversationRepository.AddAsync(conversation, cancellationToken);

            _logger.LogInformation(
                "Successfully created student group conversation {ConversationId} for student {StudentId}",
                conversation.Id, request.StudentId);

            // 9. Map to DTO and return
            var conversationDto = _mapper.Map<ConversationDto>(conversation);
            return ApiResponse<ConversationDto>.SuccessResult(conversationDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to create student group for student {StudentId}: {ErrorMessage}",
                request.StudentId, ex.Message);
            
            return ApiResponse<ConversationDto>.ErrorResult(
                $"Failed to create student group: {ex.Message}", 
                500);
        }
    }
}

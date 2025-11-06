using AutoMapper;
using Chat.Application.DTOs;
using Chat.Domain.Aggregates;
using Chat.Domain.Enums;
using Chat.Domain.Repositories;
using Chat.Domain.ValueObjects;
using EMIS.BuildingBlocks.ApiResponse;
using MediatR;

namespace Chat.Application.Commands.Conversations;

/// <summary>
/// Handler for creating student group conversations
/// </summary>
public class CreateStudentGroupCommandHandler 
    : IRequestHandler<CreateStudentGroupCommand, ApiResponse<ConversationDto>>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IMapper _mapper;

    public CreateStudentGroupCommandHandler(
        IConversationRepository conversationRepository,
        IMapper mapper)
    {
        _conversationRepository = conversationRepository;
        _mapper = mapper;
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

            // 2. Validate participants
            if (!request.ParentIds.Any())
                return ApiResponse<ConversationDto>.ErrorResult(
                    "At least one parent is required", 
                    400);

            if (!request.TeacherIds.Any())
                return ApiResponse<ConversationDto>.ErrorResult(
                    "At least one teacher is required", 
                    400);

            // 3. TODO: Fetch user names from UserService
            // For now, using placeholder names
            var teacherName = "Teacher"; // Should fetch from UserService
            var studentName = "Student"; // Should fetch from Student Service
            var parents = request.ParentIds
                .Select((parentId, index) => (parentId, $"Parent{index + 1}"))
                .ToList();

            // 4. Create conversation using factory method
            var conversation = Conversation.CreateStudentGroup(
                request.TenantId,
                request.StudentId,
                studentName,
                request.TeacherIds.First(), // Primary teacher
                teacherName,
                parents);

            // 5. Add additional teachers if any
            for (int i = 1; i < request.TeacherIds.Count; i++)
            {
                conversation.AddParticipant(
                    request.TeacherIds[i], 
                    $"Teacher{i + 1}", 
                    ParticipantRole.Admin);
            }

            // 6. Save conversation
            await _conversationRepository.AddAsync(conversation, cancellationToken);

            // 7. Map to DTO and return
            var conversationDto = _mapper.Map<ConversationDto>(conversation);
            return ApiResponse<ConversationDto>.SuccessResult(conversationDto);
        }
        catch (Exception ex)
        {
            return ApiResponse<ConversationDto>.ErrorResult(
                $"Failed to create student group: {ex.Message}", 
                500);
        }
    }
}

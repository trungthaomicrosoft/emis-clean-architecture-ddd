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
/// Handler for creating class group conversations
/// </summary>
public class CreateClassGroupCommandHandler 
    : IRequestHandler<CreateClassGroupCommand, ApiResponse<ConversationDto>>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IMapper _mapper;

    public CreateClassGroupCommandHandler(
        IConversationRepository conversationRepository,
        IMapper mapper)
    {
        _conversationRepository = conversationRepository;
        _mapper = mapper;
    }

    public async Task<ApiResponse<ConversationDto>> Handle(
        CreateClassGroupCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            // 1. Check if class group already exists
            var existingGroup = await _conversationRepository.FindClassGroupByClassIdAsync(
                request.TenantId,
                request.ClassId,
                cancellationToken);

            if (existingGroup != null)
                return ApiResponse<ConversationDto>.ErrorResult(
                    "Class group already exists for this class", 
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

            // 3. TODO: Fetch user names from UserService and Class name from Student Service
            // For now, using placeholder names
            var className = "Class"; // Should fetch from Student Service
            var teachers = request.TeacherIds
                .Select((teacherId, index) => (teacherId, $"Teacher{index + 1}"))
                .ToList();
            var parents = request.ParentIds
                .Select((parentId, index) => (parentId, $"Parent{index + 1}"))
                .ToList();

            // 4. Create conversation using factory method
            var conversation = Conversation.CreateClassGroup(
                request.TenantId,
                request.ClassId,
                className,
                teachers,
                parents);

            // 5. Save conversation
            await _conversationRepository.AddAsync(conversation, cancellationToken);

            // 6. Map to DTO and return
            var conversationDto = _mapper.Map<ConversationDto>(conversation);
            return ApiResponse<ConversationDto>.SuccessResult(conversationDto);
        }
        catch (Exception ex)
        {
            return ApiResponse<ConversationDto>.ErrorResult(
                $"Failed to create class group: {ex.Message}", 
                500);
        }
    }
}

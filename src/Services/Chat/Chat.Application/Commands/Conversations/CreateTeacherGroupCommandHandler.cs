using AutoMapper;
using Chat.Application.DTOs;
using Chat.Domain.Aggregates;
using Chat.Domain.Enums;
using Chat.Domain.Repositories;
using EMIS.BuildingBlocks.ApiResponse;
using MediatR;

namespace Chat.Application.Commands.Conversations;

/// <summary>
/// Handler for creating teacher group conversations
/// </summary>
public class CreateTeacherGroupCommandHandler 
    : IRequestHandler<CreateTeacherGroupCommand, ApiResponse<ConversationDto>>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IMapper _mapper;

    public CreateTeacherGroupCommandHandler(
        IConversationRepository conversationRepository,
        IMapper mapper)
    {
        _conversationRepository = conversationRepository;
        _mapper = mapper;
    }

    public async Task<ApiResponse<ConversationDto>> Handle(
        CreateTeacherGroupCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            // 1. Validate participants
            if (!request.TeacherIds.Any())
                return ApiResponse<ConversationDto>.ErrorResult(
                    "At least one teacher is required", 
                    400);

            // 2. TODO: Fetch teacher names from UserService
            // For now, using placeholder names
            var teachers = request.TeacherIds
                .Select((teacherId, index) => (teacherId, $"Teacher{index + 1}"))
                .ToList();

            // 3. Create conversation using factory method
            var conversation = Conversation.CreateTeacherGroup(
                request.TenantId,
                request.GroupName,
                teachers);

            // 4. Save conversation
            await _conversationRepository.AddAsync(conversation, cancellationToken);

            // 5. Map to DTO and return
            var conversationDto = _mapper.Map<ConversationDto>(conversation);
            return ApiResponse<ConversationDto>.SuccessResult(conversationDto);
        }
        catch (Exception ex)
        {
            return ApiResponse<ConversationDto>.ErrorResult(
                $"Failed to create teacher group: {ex.Message}", 
                500);
        }
    }
}

using AutoMapper;
using Chat.Application.DTOs;
using Chat.Domain.Repositories;
using EMIS.BuildingBlocks.ApiResponse;
using MediatR;

namespace Chat.Application.Queries.Conversations;

/// <summary>
/// Handler for getting conversation by ID
/// </summary>
public class GetConversationByIdQueryHandler 
    : IRequestHandler<GetConversationByIdQuery, ApiResponse<ConversationDetailDto>>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IMapper _mapper;

    public GetConversationByIdQueryHandler(
        IConversationRepository conversationRepository,
        IMapper mapper)
    {
        _conversationRepository = conversationRepository;
        _mapper = mapper;
    }

    public async Task<ApiResponse<ConversationDetailDto>> Handle(
        GetConversationByIdQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var conversation = await _conversationRepository.GetByIdAsync(
                request.ConversationId,
                cancellationToken);

            if (conversation == null)
                return ApiResponse<ConversationDetailDto>.ErrorResult("Conversation not found", 404);

            // Authorization check: User must be a participant
            if (!conversation.IsParticipant(request.UserId))
                return ApiResponse<ConversationDetailDto>.ErrorResult(
                    "You do not have access to this conversation", 
                    403);

            var dto = _mapper.Map<ConversationDetailDto>(conversation);
            dto.UnreadCount = conversation.GetUnreadCount(request.UserId);

            return ApiResponse<ConversationDetailDto>.SuccessResult(dto);
        }
        catch (Exception ex)
        {
            return ApiResponse<ConversationDetailDto>.ErrorResult(
                $"Failed to get conversation: {ex.Message}", 
                500);
        }
    }
}

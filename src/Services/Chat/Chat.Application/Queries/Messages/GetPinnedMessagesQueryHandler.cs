using AutoMapper;
using Chat.Application.DTOs;
using Chat.Domain.Repositories;
using EMIS.BuildingBlocks.ApiResponse;
using MediatR;

namespace Chat.Application.Queries.Messages;

/// <summary>
/// Handler for getting pinned messages
/// </summary>
public class GetPinnedMessagesQueryHandler 
    : IRequestHandler<GetPinnedMessagesQuery, ApiResponse<List<MessageDto>>>
{
    private readonly IMessageRepository _messageRepository;
    private readonly IConversationRepository _conversationRepository;
    private readonly IMapper _mapper;

    public GetPinnedMessagesQueryHandler(
        IMessageRepository messageRepository,
        IConversationRepository conversationRepository,
        IMapper mapper)
    {
        _messageRepository = messageRepository;
        _conversationRepository = conversationRepository;
        _mapper = mapper;
    }

    public async Task<ApiResponse<List<MessageDto>>> Handle(
        GetPinnedMessagesQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Validate conversation access
            var conversation = await _conversationRepository.GetByIdAsync(
                request.ConversationId,
                cancellationToken);

            if (conversation == null)
                return ApiResponse<List<MessageDto>>.ErrorResult("Conversation not found", 404);

            if (!conversation.IsParticipant(request.UserId))
                return ApiResponse<List<MessageDto>>.ErrorResult(
                    "You do not have access to this conversation", 
                    403);

            // Get pinned messages
            var pinnedMessages = await _messageRepository.GetPinnedMessagesAsync(
                request.ConversationId,
                cancellationToken);

            var messageDtos = pinnedMessages
                .Select(m => _mapper.Map<MessageDto>(m))
                .ToList();

            return ApiResponse<List<MessageDto>>.SuccessResult(messageDtos);
        }
        catch (Exception ex)
        {
            return ApiResponse<List<MessageDto>>.ErrorResult(
                $"Failed to get pinned messages: {ex.Message}", 
                500);
        }
    }
}

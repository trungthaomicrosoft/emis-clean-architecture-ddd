using AutoMapper;
using Chat.Application.DTOs;
using Chat.Domain.Repositories;
using EMIS.BuildingBlocks.ApiResponse;
using MediatR;

namespace Chat.Application.Queries.Messages;

/// <summary>
/// Handler for getting messages with cursor-based pagination
/// </summary>
public class GetMessagesQueryHandler 
    : IRequestHandler<GetMessagesQuery, ApiResponse<MessagesResultDto>>
{
    private readonly IMessageRepository _messageRepository;
    private readonly IConversationRepository _conversationRepository;
    private readonly IMapper _mapper;

    public GetMessagesQueryHandler(
        IMessageRepository messageRepository,
        IConversationRepository conversationRepository,
        IMapper mapper)
    {
        _messageRepository = messageRepository;
        _conversationRepository = conversationRepository;
        _mapper = mapper;
    }

    public async Task<ApiResponse<MessagesResultDto>> Handle(
        GetMessagesQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            // 1. Validate conversation access
            var conversation = await _conversationRepository.GetByIdAsync(
                request.ConversationId,
                cancellationToken);

            if (conversation == null)
                return ApiResponse<MessagesResultDto>.ErrorResult("Conversation not found", 404);

            if (!conversation.IsParticipant(request.UserId))
                return ApiResponse<MessagesResultDto>.ErrorResult(
                    "You do not have access to this conversation", 
                    403);

            // 2. Get messages with cursor-based pagination
            var (messages, hasMore) = await _messageRepository.GetMessagesAsync(
                request.ConversationId,
                request.BeforeTimestamp,
                request.PageSize,
                cancellationToken);

            // 3. Map to DTOs
            var messageDtos = messages.Select(m => _mapper.Map<MessageDto>(m)).ToList();

            // 4. Get oldest message timestamp for next cursor
            var oldestTimestamp = messageDtos.Any() 
                ? messageDtos.Last().SentAt 
                : (DateTime?)null;

            var result = new MessagesResultDto
            {
                Messages = messageDtos,
                HasMore = hasMore,
                OldestMessageTimestamp = oldestTimestamp
            };

            return ApiResponse<MessagesResultDto>.SuccessResult(result);
        }
        catch (Exception ex)
        {
            return ApiResponse<MessagesResultDto>.ErrorResult(
                $"Failed to get messages: {ex.Message}", 
                500);
        }
    }
}

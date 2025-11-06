using AutoMapper;
using Chat.Application.DTOs;
using Chat.Domain.Repositories;
using EMIS.BuildingBlocks.ApiResponse;
using EMIS.BuildingBlocks.Pagination;
using MediatR;

namespace Chat.Application.Queries.Messages;

/// <summary>
/// Handler for getting messages by type (media gallery)
/// </summary>
public class GetMessagesByTypeQueryHandler 
    : IRequestHandler<GetMessagesByTypeQuery, ApiResponse<PagedResult<MessageDto>>>
{
    private readonly IMessageRepository _messageRepository;
    private readonly IConversationRepository _conversationRepository;
    private readonly IMapper _mapper;

    public GetMessagesByTypeQueryHandler(
        IMessageRepository messageRepository,
        IConversationRepository conversationRepository,
        IMapper mapper)
    {
        _messageRepository = messageRepository;
        _conversationRepository = conversationRepository;
        _mapper = mapper;
    }

    public async Task<ApiResponse<PagedResult<MessageDto>>> Handle(
        GetMessagesByTypeQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Validate conversation access
            var conversation = await _conversationRepository.GetByIdAsync(
                request.ConversationId,
                cancellationToken);

            if (conversation == null)
                return ApiResponse<PagedResult<MessageDto>>.ErrorResult("Conversation not found", 404);

            if (!conversation.IsParticipant(request.UserId))
                return ApiResponse<PagedResult<MessageDto>>.ErrorResult(
                    "You do not have access to this conversation", 
                    403);

            // Get messages by type
            var (messages, totalCount) = await _messageRepository.GetMessagesByTypeAsync(
                request.ConversationId,
                request.MessageType,
                request.PageNumber,
                request.PageSize,
                cancellationToken);

            var messageDtos = messages.Select(m => _mapper.Map<MessageDto>(m)).ToList();

            var pagedResult = new PagedResult<MessageDto>(
                messageDtos,
                totalCount,
                request.PageNumber,
                request.PageSize);

            return ApiResponse<PagedResult<MessageDto>>.SuccessResult(pagedResult);
        }
        catch (Exception ex)
        {
            return ApiResponse<PagedResult<MessageDto>>.ErrorResult(
                $"Failed to get messages by type: {ex.Message}", 
                500);
        }
    }
}

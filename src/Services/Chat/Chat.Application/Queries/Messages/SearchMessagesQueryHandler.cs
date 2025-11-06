using AutoMapper;
using Chat.Application.DTOs;
using Chat.Domain.Repositories;
using EMIS.BuildingBlocks.ApiResponse;
using EMIS.BuildingBlocks.Pagination;
using MediatR;

namespace Chat.Application.Queries.Messages;

/// <summary>
/// Handler for searching messages
/// Uses MongoDB text search (Phase 1)
/// Can be replaced with Elasticsearch for better relevance (Phase 2)
/// </summary>
public class SearchMessagesQueryHandler 
    : IRequestHandler<SearchMessagesQuery, ApiResponse<PagedResult<MessageDto>>>
{
    private readonly IMessageRepository _messageRepository;
    private readonly IConversationRepository _conversationRepository;
    private readonly IMapper _mapper;

    public SearchMessagesQueryHandler(
        IMessageRepository messageRepository,
        IConversationRepository conversationRepository,
        IMapper mapper)
    {
        _messageRepository = messageRepository;
        _conversationRepository = conversationRepository;
        _mapper = mapper;
    }

    public async Task<ApiResponse<PagedResult<MessageDto>>> Handle(
        SearchMessagesQuery request,
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

            // Validate search term
            if (string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                return ApiResponse<PagedResult<MessageDto>>.SuccessResult(
                    new PagedResult<MessageDto>(
                        new List<MessageDto>(), 
                        0, 
                        request.PageNumber, 
                        request.PageSize));
            }

            // Search messages
            var (messages, totalCount) = await _messageRepository.SearchMessagesAsync(
                request.ConversationId,
                request.SearchTerm,
                request.FilterByType,
                request.FromDate,
                request.ToDate,
                request.PageNumber,
                request.PageSize,
                cancellationToken);

            // Map to DTOs
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
                $"Failed to search messages: {ex.Message}", 
                500);
        }
    }
}

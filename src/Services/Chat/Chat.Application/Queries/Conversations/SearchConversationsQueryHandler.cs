using AutoMapper;
using Chat.Application.DTOs;
using Chat.Domain.Repositories;
using EMIS.BuildingBlocks.ApiResponse;
using MediatR;

namespace Chat.Application.Queries.Conversations;

/// <summary>
/// Handler for searching conversations by name
/// </summary>
public class SearchConversationsQueryHandler 
    : IRequestHandler<SearchConversationsQuery, ApiResponse<List<ConversationDto>>>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IMapper _mapper;

    public SearchConversationsQueryHandler(
        IConversationRepository conversationRepository,
        IMapper mapper)
    {
        _conversationRepository = conversationRepository;
        _mapper = mapper;
    }

    public async Task<ApiResponse<List<ConversationDto>>> Handle(
        SearchConversationsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.SearchTerm))
                return ApiResponse<List<ConversationDto>>.SuccessResult(new List<ConversationDto>());

            var (conversations, _) = await _conversationRepository.SearchConversationsByNameAsync(
                request.UserId,
                request.TenantId,
                request.SearchTerm,
                request.PageNumber,
                request.PageSize,
                cancellationToken);

            var conversationDtos = conversations.Select(c =>
            {
                var dto = _mapper.Map<ConversationDto>(c);
                dto.UnreadCount = c.GetUnreadCount(request.UserId);
                return dto;
            }).ToList();

            return ApiResponse<List<ConversationDto>>.SuccessResult(conversationDtos);
        }
        catch (Exception ex)
        {
            return ApiResponse<List<ConversationDto>>.ErrorResult(
                $"Failed to search conversations: {ex.Message}", 
                500);
        }
    }
}

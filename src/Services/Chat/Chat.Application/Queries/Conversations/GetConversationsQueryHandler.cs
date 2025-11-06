using AutoMapper;
using Chat.Application.DTOs;
using Chat.Domain.Repositories;
using EMIS.BuildingBlocks.ApiResponse;
using EMIS.BuildingBlocks.Pagination;
using MediatR;

namespace Chat.Application.Queries.Conversations;

/// <summary>
/// Handler for getting user's conversations with pagination
/// </summary>
public class GetConversationsQueryHandler 
    : IRequestHandler<GetConversationsQuery, ApiResponse<PagedResult<ConversationDto>>>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IMapper _mapper;

    public GetConversationsQueryHandler(
        IConversationRepository conversationRepository,
        IMapper mapper)
    {
        _conversationRepository = conversationRepository;
        _mapper = mapper;
    }

    public async Task<ApiResponse<PagedResult<ConversationDto>>> Handle(
        GetConversationsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get conversations with pagination
            var (conversations, totalCount) = await _conversationRepository.GetUserConversationsAsync(
                request.UserId,
                request.TenantId,
                request.FilterByType,
                request.IncludeArchived,
                request.PageNumber,
                request.PageSize,
                cancellationToken);

            // Map to DTOs and set unread count per user
            var conversationDtos = conversations.Select(c =>
            {
                var dto = _mapper.Map<ConversationDto>(c);
                // Set unread count for this specific user
                dto.UnreadCount = c.GetUnreadCount(request.UserId);
                return dto;
            }).ToList();

            // Create paged result
            var pagedResult = new PagedResult<ConversationDto>(
                conversationDtos,
                totalCount,
                request.PageNumber,
                request.PageSize);

            return ApiResponse<PagedResult<ConversationDto>>.SuccessResult(pagedResult);
        }
        catch (Exception ex)
        {
            return ApiResponse<PagedResult<ConversationDto>>.ErrorResult(
                $"Failed to get conversations: {ex.Message}", 
                500);
        }
    }
}

using Chat.Domain.Repositories;
using EMIS.BuildingBlocks.ApiResponse;
using MediatR;

namespace Chat.Application.Queries.Conversations;

/// <summary>
/// Handler for getting total unread conversations count
/// Used for badge notifications
/// </summary>
public class GetUnreadCountQueryHandler 
    : IRequestHandler<GetUnreadCountQuery, ApiResponse<int>>
{
    private readonly IConversationRepository _conversationRepository;

    public GetUnreadCountQueryHandler(IConversationRepository conversationRepository)
    {
        _conversationRepository = conversationRepository;
    }

    public async Task<ApiResponse<int>> Handle(
        GetUnreadCountQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var unreadCount = await _conversationRepository.GetUnreadConversationsCountAsync(
                request.UserId,
                request.TenantId,
                cancellationToken);

            return ApiResponse<int>.SuccessResult(unreadCount);
        }
        catch (Exception ex)
        {
            return ApiResponse<int>.ErrorResult(
                $"Failed to get unread count: {ex.Message}", 
                500);
        }
    }
}

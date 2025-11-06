using Chat.Application.DTOs;
using Chat.Domain.Enums;
using EMIS.BuildingBlocks.ApiResponse;
using EMIS.BuildingBlocks.Pagination;
using MediatR;

namespace Chat.Application.Queries.Messages;

/// <summary>
/// Query to get messages by type (images, videos, files, etc.)
/// Useful for media gallery view
/// </summary>
public class GetMessagesByTypeQuery : IRequest<ApiResponse<PagedResult<MessageDto>>>
{
    public Guid ConversationId { get; set; }
    public Guid UserId { get; set; } // For authorization
    public MessageType MessageType { get; set; } // Image, Video, File, Audio
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

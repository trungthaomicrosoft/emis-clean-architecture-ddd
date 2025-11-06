using Chat.Application.DTOs;
using EMIS.BuildingBlocks.ApiResponse;
using MediatR;

namespace Chat.Application.Commands.Messages;

/// <summary>
/// Command to send a text message
/// </summary>
public class SendTextMessageCommand : IRequest<ApiResponse<MessageDto>>
{
    public Guid ConversationId { get; set; }
    public Guid SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    
    // Optional: Reply to another message
    public Guid? ReplyToMessageId { get; set; }
    
    // Optional: Mentions
    public List<MentionDto> Mentions { get; set; } = new();
}

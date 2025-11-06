using Chat.Application.DTOs;
using Chat.Domain.Enums;
using EMIS.BuildingBlocks.ApiResponse;
using MediatR;

namespace Chat.Application.Commands.Messages;

/// <summary>
/// Command to send a message with attachment (image, video, audio, file)
/// Handles file upload validation and storage
/// Note: File property uses 'object' to avoid coupling Application layer to ASP.NET Core
/// API layer will pass IFormFile, which will be cast in handler
/// </summary>
public class SendAttachmentMessageCommand : IRequest<ApiResponse<MessageDto>>
{
    public Guid ConversationId { get; set; }
    public Guid SenderId { get; set; }
    public Guid TenantId { get; set; }
    public MessageType MessageType { get; set; } // Image, Video, Audio, File
    public object File { get; set; } = null!; // The uploaded file (IFormFile from API layer)
    public string? Caption { get; set; } // Optional caption for the attachment
    
    // Reply context
    public Guid? ReplyToMessageId { get; set; }
    
    // Mentions
    public List<Guid> MentionedUserIds { get; set; } = new();
}

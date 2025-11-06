using Chat.Application.DTOs;
using EMIS.BuildingBlocks.ApiResponse;
using MediatR;

namespace Chat.Application.Commands.Conversations;

/// <summary>
/// Command to create an announcement channel
/// Read-only channel where only admins/teachers can post, parents can only read
/// </summary>
public class CreateAnnouncementChannelCommand : IRequest<ApiResponse<ConversationDto>>
{
    public Guid TenantId { get; set; }
    public string ChannelName { get; set; } = string.Empty; // e.g., "School Announcements"
    
    // Participants
    public List<Guid> AdminIds { get; set; } = new(); // Admins who can post
    public List<Guid> ParentIds { get; set; } = new(); // Parents (read-only)
    
    public Guid CreatedBy { get; set; } // User creating the channel (usually admin)
}

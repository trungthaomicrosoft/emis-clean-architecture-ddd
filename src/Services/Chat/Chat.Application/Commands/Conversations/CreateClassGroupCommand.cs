using Chat.Application.DTOs;
using EMIS.BuildingBlocks.ApiResponse;
using MediatR;

namespace Chat.Application.Commands.Conversations;

/// <summary>
/// Command to create a class group conversation
/// Includes all parents + teachers in a class
/// Auto-created when new class is created
/// </summary>
public class CreateClassGroupCommand : IRequest<ApiResponse<ConversationDto>>
{
    public Guid TenantId { get; set; }
    public Guid ClassId { get; set; } // Class this group is for
    public string GroupName { get; set; } = string.Empty; // e.g., "Class Group: Lớp Chồi 1A"
    
    // Participants
    public List<Guid> ParentIds { get; set; } = new(); // All parents in the class
    public List<Guid> TeacherIds { get; set; } = new(); // Class teachers
    
    public Guid CreatedBy { get; set; } // User creating the group (usually system or admin)
}

using Chat.Application.DTOs;
using EMIS.BuildingBlocks.ApiResponse;
using MediatR;

namespace Chat.Application.Commands.Conversations;

/// <summary>
/// Command to create a teacher group conversation
/// Includes all teachers in the school (or department)
/// </summary>
public class CreateTeacherGroupCommand : IRequest<ApiResponse<ConversationDto>>
{
    public Guid TenantId { get; set; }
    public string GroupName { get; set; } = string.Empty; // e.g., "Teacher Group"
    
    // Participants
    public List<Guid> TeacherIds { get; set; } = new(); // All teachers
    
    public Guid CreatedBy { get; set; } // User creating the group (usually admin)
}

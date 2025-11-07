using Chat.Application.DTOs;
using EMIS.BuildingBlocks.ApiResponse;
using MediatR;

namespace Chat.Application.Commands.Conversations;

/// <summary>
/// Command to create a student group conversation (for a specific student)
/// Backend will automatically fetch parents and teachers based on StudentId and ClassId
/// Auto-created when new parent enrolls
/// </summary>
public class CreateStudentGroupCommand : IRequest<ApiResponse<ConversationDto>>
{
    public Guid TenantId { get; set; }
    public Guid StudentId { get; set; } // Student this group is about
    public Guid ClassId { get; set; } // Student's class
    public string GroupName { get; set; } = string.Empty; // e.g., "Group: Nguyễn Văn A"
    public Guid CreatedBy { get; set; } // User creating the group (usually system or admin)
}

using Chat.Application.DTOs;
using EMIS.BuildingBlocks.ApiResponse;
using MediatR;

namespace Chat.Application.Commands.Conversations;

/// <summary>
/// Command to create a student group conversation from an integration event
/// This is triggered automatically when a new student is created
/// Different from CreateStudentGroupCommand which is triggered by user action
/// </summary>
public class CreateStudentGroupFromEventCommand : IRequest<ApiResponse<ConversationDto>>
{
    public Guid TenantId { get; set; }
    public Guid StudentId { get; set; }
    public Guid ClassId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    
    /// <summary>
    /// Parents info already provided by the event, no need to fetch from Student Service
    /// </summary>
    public List<(Guid ParentId, string ParentName)> Parents { get; set; } = new();
    
    public Guid CreatedBy { get; set; }
}

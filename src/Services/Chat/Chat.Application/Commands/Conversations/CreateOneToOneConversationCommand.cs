using EMIS.BuildingBlocks.ApiResponse;
using MediatR;

namespace Chat.Application.Commands.Conversations;

/// <summary>
/// Command to create a OneToOne conversation
/// </summary>
public class CreateOneToOneConversationCommand : IRequest<ApiResponse<Guid>>
{
    public Guid TenantId { get; set; }
    public Guid User1Id { get; set; }
    public string User1Name { get; set; } = string.Empty;
    public Guid User2Id { get; set; }
    public string User2Name { get; set; } = string.Empty;
}

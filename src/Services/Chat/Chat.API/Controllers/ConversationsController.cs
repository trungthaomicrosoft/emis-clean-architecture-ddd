using Chat.Application.Commands.Conversations;
using Chat.Application.DTOs;
using Chat.Application.Queries.Conversations;
using EMIS.BuildingBlocks.ApiResponse;
using EMIS.BuildingBlocks.Pagination;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Chat.API.Controllers;

/// <summary>
/// Controller for managing conversations
/// Handles CRUD operations, participants, and conversation queries
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class ConversationsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ConversationsController> _logger;

    public ConversationsController(
        IMediator mediator,
        ILogger<ConversationsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    #region Conversation Creation

    /// <summary>
    /// Create a one-to-one conversation between two users
    /// </summary>
    [HttpPost("one-to-one")]
    public async Task<ActionResult<ApiResponse<ConversationDto>>> CreateOneToOne(
        [FromBody] CreateOneToOneConversationCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Create a student group conversation (parent + teachers)
    /// </summary>
    [HttpPost("student-group")]
    public async Task<ActionResult<ApiResponse<ConversationDto>>> CreateStudentGroup(
        [FromBody] CreateStudentGroupCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Create a class group conversation (all parents + teachers)
    /// </summary>
    [HttpPost("class-group")]
    public async Task<ActionResult<ApiResponse<ConversationDto>>> CreateClassGroup(
        [FromBody] CreateClassGroupCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Create a teacher group conversation
    /// </summary>
    [HttpPost("teacher-group")]
    public async Task<ActionResult<ApiResponse<ConversationDto>>> CreateTeacherGroup(
        [FromBody] CreateTeacherGroupCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Create an announcement channel (read-only for parents)
    /// </summary>
    [HttpPost("announcement-channel")]
    public async Task<ActionResult<ApiResponse<ConversationDto>>> CreateAnnouncementChannel(
        [FromBody] CreateAnnouncementChannelCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    #endregion

    #region Conversation Queries

    /// <summary>
    /// Get conversation by ID
    /// </summary>
    [HttpGet("{conversationId:guid}")]
    public async Task<ActionResult<ApiResponse<ConversationDetailDto>>> GetById(
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        var query = new GetConversationByIdQuery { ConversationId = conversationId };
        var result = await _mediator.Send(query, cancellationToken);
        
        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>
    /// Get user's conversations with pagination and filters
    /// </summary>
    [HttpGet("user/{userId:guid}")]
    public async Task<ActionResult<ApiResponse<PagedResult<ConversationDto>>>> GetUserConversations(
        Guid userId,
        [FromQuery] Guid tenantId,
        [FromQuery] string? type = null,
        [FromQuery] bool includeArchived = false,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetConversationsQuery
        {
            UserId = userId,
            TenantId = tenantId,
            IncludeArchived = includeArchived,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Search conversations by name
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<ApiResponse<List<ConversationDto>>>> Search(
        [FromQuery] Guid userId,
        [FromQuery] string searchTerm,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new SearchConversationsQuery
        {
            UserId = userId,
            SearchTerm = searchTerm,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get conversations for a specific student
    /// </summary>
    [HttpGet("student/{studentId:guid}")]
    public async Task<ActionResult<ApiResponse<List<ConversationDto>>>> GetStudentConversations(
        Guid studentId,
        [FromQuery] Guid tenantId,
        CancellationToken cancellationToken)
    {
        // TODO: Implement GetStudentConversationsQuery
        return Ok(ApiResponse<List<ConversationDto>>.SuccessResult(new List<ConversationDto>()));
    }

    /// <summary>
    /// Get conversations for a specific class
    /// </summary>
    [HttpGet("class/{classId:guid}")]
    public async Task<ActionResult<ApiResponse<List<ConversationDto>>>> GetClassConversations(
        Guid classId,
        [FromQuery] Guid tenantId,
        CancellationToken cancellationToken)
    {
        // TODO: Implement GetClassConversationsQuery
        return Ok(ApiResponse<List<ConversationDto>>.SuccessResult(new List<ConversationDto>()));
    }

    #endregion

    #region Participant Management

    /// <summary>
    /// Add participant to a conversation
    /// </summary>
    [HttpPost("{conversationId:guid}/participants")]
    public async Task<ActionResult<ApiResponse<bool>>> AddParticipant(
        Guid conversationId,
        [FromBody] AddParticipantCommand command,
        CancellationToken cancellationToken)
    {
        command.ConversationId = conversationId;
        var result = await _mediator.Send(command, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Remove participant from a conversation
    /// </summary>
    [HttpDelete("{conversationId:guid}/participants/{userIdToRemove:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> RemoveParticipant(
        Guid conversationId,
        Guid userIdToRemove,
        [FromQuery] Guid requestedBy,
        CancellationToken cancellationToken)
    {
        var command = new RemoveParticipantCommand
        {
            ConversationId = conversationId,
            UserIdToRemove = userIdToRemove,
            RequestedBy = requestedBy
        };

        var result = await _mediator.Send(command, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Leave a conversation
    /// </summary>
    [HttpPost("{conversationId:guid}/leave")]
    public async Task<ActionResult<ApiResponse<bool>>> LeaveConversation(
        Guid conversationId,
        [FromQuery] Guid userId,
        CancellationToken cancellationToken)
    {
        // Use RemoveParticipantCommand
        var command = new RemoveParticipantCommand
        {
            ConversationId = conversationId,
            UserIdToRemove = userId,
            RequestedBy = userId // User leaving themselves
        };

        var result = await _mediator.Send(command, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    #endregion

    #region Conversation Settings

    /// <summary>
    /// Archive a conversation
    /// </summary>
    [HttpPut("{conversationId:guid}/archive")]
    public async Task<ActionResult<ApiResponse<bool>>> ArchiveConversation(
        Guid conversationId,
        [FromQuery] Guid userId,
        CancellationToken cancellationToken)
    {
        // TODO: Implement ArchiveConversationCommand
        return Ok(ApiResponse<bool>.SuccessResult(true));
    }

    /// <summary>
    /// Unarchive a conversation
    /// </summary>
    [HttpPut("{conversationId:guid}/unarchive")]
    public async Task<ActionResult<ApiResponse<bool>>> UnarchiveConversation(
        Guid conversationId,
        [FromQuery] Guid userId,
        CancellationToken cancellationToken)
    {
        // TODO: Create UnarchiveConversationCommand
        return Ok(ApiResponse<bool>.SuccessResult(true));
    }

    #endregion
}

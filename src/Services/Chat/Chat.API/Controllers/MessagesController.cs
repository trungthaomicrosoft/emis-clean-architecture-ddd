using Chat.Application.Commands.Messages;
using Chat.Application.DTOs;
using Chat.Application.Queries.Messages;
using EMIS.BuildingBlocks.ApiResponse;
using EMIS.BuildingBlocks.Pagination;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Chat.API.Controllers;

/// <summary>
/// Controller for managing messages
/// Handles message CRUD, reactions, pinning, and queries
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class MessagesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<MessagesController> _logger;

    public MessagesController(
        IMediator mediator,
        ILogger<MessagesController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    #region Message Creation

    /// <summary>
    /// Send a text message
    /// </summary>
    [HttpPost("text")]
    public async Task<ActionResult<ApiResponse<MessageDto>>> SendText(
        [FromBody] SendTextMessageCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Send a message with file attachment
    /// </summary>
    [HttpPost("attachment")]
    [RequestSizeLimit(26214400)] // 25MB
    public async Task<ActionResult<ApiResponse<MessageDto>>> SendAttachment(
        [FromForm] IFormFile file,
        [FromForm] Guid conversationId,
        [FromForm] Guid senderId,
        [FromForm] string? caption,
        CancellationToken cancellationToken)
    {
        var command = new SendAttachmentMessageCommand
        {
            ConversationId = conversationId,
            SenderId = senderId,
            File = file,
            Caption = caption
        };

        var result = await _mediator.Send(command, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    #endregion

    #region Message Queries

    /// <summary>
    /// Get messages in a conversation with cursor-based pagination
    /// </summary>
    [HttpGet("conversation/{conversationId:guid}")]
    public async Task<ActionResult<ApiResponse<PagedResult<MessageDto>>>> GetMessages(
        Guid conversationId,
        [FromQuery] DateTime? beforeTimestamp = null,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var query = new GetMessagesQuery
        {
            ConversationId = conversationId,
            BeforeTimestamp = beforeTimestamp,
            PageSize = pageSize
        };

        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get a specific message by ID
    /// </summary>
    [HttpGet("{messageId:guid}")]
    public async Task<ActionResult<ApiResponse<MessageDto>>> GetById(
        Guid messageId,
        CancellationToken cancellationToken)
    {
        // TODO: Implement GetMessageByIdQuery
        return NotFound(ApiResponse<MessageDto>.ErrorResult("Not implemented"));
    }

    /// <summary>
    /// Search messages in a conversation
    /// </summary>
    [HttpGet("conversation/{conversationId:guid}/search")]
    public async Task<ActionResult<ApiResponse<PagedResult<MessageDto>>>> Search(
        Guid conversationId,
        [FromQuery] string searchTerm,
        [FromQuery] string? messageType = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new SearchMessagesQuery
        {
            ConversationId = conversationId,
            UserId = Guid.Empty, // TODO: Get from context
            SearchTerm = searchTerm,
            FilterByType = string.IsNullOrEmpty(messageType) ? null : Enum.Parse<Chat.Domain.Enums.MessageType>(messageType),
            FromDate = fromDate,
            ToDate = toDate,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get pinned messages in a conversation
    /// </summary>
    [HttpGet("conversation/{conversationId:guid}/pinned")]
    public async Task<ActionResult<ApiResponse<List<MessageDto>>>> GetPinned(
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        var query = new GetPinnedMessagesQuery { ConversationId = conversationId };
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get messages by type (for media gallery)
    /// </summary>
    [HttpGet("conversation/{conversationId:guid}/type/{messageType}")]
    public async Task<ActionResult<ApiResponse<PagedResult<MessageDto>>>> GetByType(
        Guid conversationId,
        string messageType,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetMessagesByTypeQuery
        {
            ConversationId = conversationId,
            UserId = Guid.Empty, // TODO: Get from context
            MessageType = Enum.Parse<Chat.Domain.Enums.MessageType>(messageType),
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get messages mentioning a specific user
    /// </summary>
    [HttpGet("conversation/{conversationId:guid}/mentions/{userId:guid}")]
    public async Task<ActionResult<ApiResponse<List<MessageDto>>>> GetMentions(
        Guid conversationId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        // TODO: Implement GetMessagesMentioningUserQuery
        return Ok(ApiResponse<List<MessageDto>>.SuccessResult(new List<MessageDto>()));
    }

    #endregion

    #region Message Operations

    /// <summary>
    /// Edit a message
    /// </summary>
    [HttpPut("{messageId:guid}")]
    public async Task<ActionResult<ApiResponse<MessageDto>>> Edit(
        Guid messageId,
        [FromBody] EditMessageCommand command,
        CancellationToken cancellationToken)
    {
        command.MessageId = messageId;
        var result = await _mediator.Send(command, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Delete a message
    /// </summary>
    [HttpDelete("{messageId:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(
        Guid messageId,
        [FromQuery] Guid userId,
        CancellationToken cancellationToken)
    {
        var command = new DeleteMessageCommand
        {
            MessageId = messageId,
            DeleterUserId = userId
        };

        var result = await _mediator.Send(command, cancellationToken);
        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>
    /// Pin a message
    /// </summary>
    [HttpPost("{messageId:guid}/pin")]
    public async Task<ActionResult<ApiResponse<bool>>> Pin(
        Guid messageId,
        [FromQuery] Guid userId,
        CancellationToken cancellationToken)
    {
        var command = new PinMessageCommand
        {
            MessageId = messageId,
            PinnedByUserId = userId
        };

        var result = await _mediator.Send(command, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Unpin a message
    /// </summary>
    [HttpPost("{messageId:guid}/unpin")]
    public async Task<ActionResult<ApiResponse<bool>>> Unpin(
        Guid messageId,
        [FromQuery] Guid userId,
        CancellationToken cancellationToken)
    {
        // TODO: Implement UnpinMessageCommand or use PinMessageCommand with flag
        return Ok(ApiResponse<bool>.SuccessResult(true));
    }

    /// <summary>
    /// Add reaction to a message
    /// </summary>
    [HttpPost("{messageId:guid}/reactions")]
    public async Task<ActionResult<ApiResponse<MessageDto>>> AddReaction(
        Guid messageId,
        [FromBody] AddReactionCommand command,
        CancellationToken cancellationToken)
    {
        command.MessageId = messageId;
        var result = await _mediator.Send(command, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Remove reaction from a message
    /// </summary>
    [HttpDelete("{messageId:guid}/reactions")]
    public async Task<ActionResult<ApiResponse<MessageDto>>> RemoveReaction(
        Guid messageId,
        [FromQuery] Guid userId,
        [FromQuery] string emoji,
        CancellationToken cancellationToken)
    {
        // TODO: Implement RemoveReactionCommand
        return Ok(ApiResponse<MessageDto>.SuccessResult(new MessageDto()));
    }

    /// <summary>
    /// Mark messages as read in a conversation
    /// </summary>
    [HttpPost("conversation/{conversationId:guid}/read")]
    public async Task<ActionResult<ApiResponse<bool>>> MarkAsRead(
        Guid conversationId,
        [FromQuery] Guid userId,
        CancellationToken cancellationToken)
    {
        var command = new MarkMessagesAsReadCommand
        {
            ConversationId = conversationId,
            UserId = userId
        };

        var result = await _mediator.Send(command, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    #endregion
}

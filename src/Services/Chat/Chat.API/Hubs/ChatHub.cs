using Chat.Application.Commands.Messages;
using Chat.Application.DTOs;
using Chat.Application.Interfaces;
using Chat.Application.Queries.Messages;
using Chat.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Chat.API.Hubs;

/// <summary>
/// SignalR Hub for real-time chat messaging
/// Handles message sending, typing indicators, online status, read receipts
/// </summary>
[Authorize]
public class ChatHub : Hub
{
    private readonly IMediator _mediator;
    private readonly ICacheService _cacheService;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(
        IMediator mediator,
        ICacheService cacheService,
        ILogger<ChatHub> logger)
    {
        _mediator = mediator;
        _cacheService = cacheService;
        _logger = logger;
    }

    #region Connection Management

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        var tenantId = GetTenantId();

        _logger.LogInformation("User {UserId} connected to ChatHub", userId);

        // Add user to online users cache
        await _cacheService.AddOnlineUserAsync(tenantId, userId);

        // Join user's conversation groups
        await JoinUserConversations(userId);

        // Notify others that user is online
        await Clients.Others.SendAsync("UserOnline", new { UserId = userId });

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        var tenantId = GetTenantId();

        _logger.LogInformation("User {UserId} disconnected from ChatHub", userId);

        // Remove user from online users cache
        await _cacheService.RemoveOnlineUserAsync(tenantId, userId);

        // Notify others that user is offline
        await Clients.Others.SendAsync("UserOffline", new { UserId = userId });

        await base.OnDisconnectedAsync(exception);
    }

    #endregion

    #region Message Operations

    /// <summary>
    /// Send a text message to a conversation
    /// </summary>
    public async Task SendMessage(SendMessageRequest request)
    {
        try
        {
            var userId = GetUserId();
            var tenantId = GetTenantId();

            var command = new SendTextMessageCommand
            {
                ConversationId = request.ConversationId,
                SenderId = userId,
                SenderName = "User", // TODO: Fetch from context
                Content = request.Content,
                ReplyToMessageId = request.ReplyToMessageId
            };

            var result = await _mediator.Send(command);

            if (result.Success && result.Data != null)
            {
                // Send message to all users in the conversation
                await Clients.Group(request.ConversationId.ToString())
                    .SendAsync("ReceiveMessage", result.Data);

                // Increment unread counts for other participants
                await UpdateUnreadCounts(request.ConversationId, userId);
            }
            else
            {
                await Clients.Caller.SendAsync("MessageError", new { Error = result.Error?.Message ?? "Failed to send message" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message");
            await Clients.Caller.SendAsync("MessageError", new { Error = ex.Message });
        }
    }

    /// <summary>
    /// Edit an existing message
    /// </summary>
    public async Task EditMessage(Guid messageId, string newContent)
    {
        try
        {
            var userId = GetUserId();

            var command = new EditMessageCommand
            {
                MessageId = messageId,
                EditorUserId = userId,
                NewContent = newContent
            };

            var result = await _mediator.Send(command);

            if (result.Success)
            {
                // Notify about edit - get conversation from cache or query
                await Clients.Caller.SendAsync("MessageEdited", new { MessageId = messageId });
            }
            else
            {
                await Clients.Caller.SendAsync("MessageError", new { Error = result.Error?.Message ?? "Edit failed" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error editing message");
            await Clients.Caller.SendAsync("MessageError", new { Error = ex.Message });
        }
    }

    /// <summary>
    /// Delete a message
    /// </summary>
    public async Task DeleteMessage(Guid messageId)
    {
        try
        {
            var userId = GetUserId();

            var command = new DeleteMessageCommand
            {
                MessageId = messageId,
                DeleterUserId = userId
            };

            var result = await _mediator.Send(command);

            if (result.Success)
            {
                // Get conversation ID from cache or query
                await Clients.Group("conversation_group")
                    .SendAsync("MessageDeleted", new { MessageId = messageId });
            }
            else
            {
                await Clients.Caller.SendAsync("MessageError", new { Error = result.Error?.Message ?? "Delete failed" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting message");
            await Clients.Caller.SendAsync("MessageError", new { Error = ex.Message });
        }
    }

    /// <summary>
    /// React to a message
    /// </summary>
    public async Task ReactToMessage(Guid messageId, string emoji)
    {
        try
        {
            var userId = GetUserId();

            var command = new AddReactionCommand
            {
                MessageId = messageId,
                UserId = userId,
                UserName = "User", // TODO: Fetch from context
                EmojiCode = emoji
            };

            var result = await _mediator.Send(command);

            if (result.Success)
            {
                await Clients.Caller.SendAsync("MessageReacted", new { MessageId = messageId, Emoji = emoji });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reacting to message");
        }
    }

    #endregion

    #region Typing Indicators

    /// <summary>
    /// Notify others that user is typing
    /// </summary>
    public async Task StartTyping(Guid conversationId)
    {
        try
        {
            var userId = GetUserId();
            
            // Add to typing users cache (5 second TTL)
            await _cacheService.AddTypingUserAsync(conversationId, userId);

            // Notify others in the conversation
            await Clients.OthersInGroup(conversationId.ToString())
                .SendAsync("UserTyping", new { ConversationId = conversationId, UserId = userId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in StartTyping");
        }
    }

    /// <summary>
    /// Notify others that user stopped typing
    /// </summary>
    public async Task StopTyping(Guid conversationId)
    {
        try
        {
            var userId = GetUserId();
            
            // Remove from typing users cache
            await _cacheService.RemoveTypingUserAsync(conversationId, userId);

            // Notify others in the conversation
            await Clients.OthersInGroup(conversationId.ToString())
                .SendAsync("UserStoppedTyping", new { ConversationId = conversationId, UserId = userId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in StopTyping");
        }
    }

    #endregion

    #region Read Receipts

    /// <summary>
    /// Mark messages as read in a conversation
    /// </summary>
    public async Task MarkAsRead(Guid conversationId)
    {
        try
        {
            var userId = GetUserId();

            var command = new MarkMessagesAsReadCommand
            {
                ConversationId = conversationId,
                UserId = userId
            };

            var result = await _mediator.Send(command);

            if (result.Success)
            {
                // Reset unread count in cache
                await _cacheService.ResetUnreadCountAsync(userId, conversationId);

                // Notify others about read receipt
                await Clients.OthersInGroup(conversationId.ToString())
                    .SendAsync("MessagesRead", new 
                    { 
                        ConversationId = conversationId, 
                        UserId = userId,
                        ReadAt = DateTime.UtcNow
                    });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking messages as read");
        }
    }

    #endregion

    #region Group Management

    /// <summary>
    /// Join a conversation group (for receiving real-time updates)
    /// </summary>
    public async Task JoinConversation(Guid conversationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, conversationId.ToString());
        _logger.LogInformation("User {UserId} joined conversation {ConversationId}", 
            GetUserId(), conversationId);
    }

    /// <summary>
    /// Leave a conversation group
    /// </summary>
    public async Task LeaveConversation(Guid conversationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, conversationId.ToString());
        _logger.LogInformation("User {UserId} left conversation {ConversationId}", 
            GetUserId(), conversationId);
    }

    #endregion

    #region Helper Methods

    private Guid GetUserId()
    {
        var userIdClaim = Context.User?.FindFirst("UserId")?.Value 
            ?? Context.User?.FindFirst("sub")?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new HubException("User ID not found in token");
        }
        
        return userId;
    }

    private Guid GetTenantId()
    {
        var tenantIdClaim = Context.User?.FindFirst("TenantId")?.Value;
        
        if (string.IsNullOrEmpty(tenantIdClaim) || !Guid.TryParse(tenantIdClaim, out var tenantId))
        {
            throw new HubException("Tenant ID not found in token");
        }
        
        return tenantId;
    }

    private async Task JoinUserConversations(Guid userId)
    {
        try
        {
            // Get user's conversations from cache
            var conversationIds = await _cacheService.GetUserConversationsAsync(userId);
            
            if (conversationIds != null)
            {
                foreach (var conversationId in conversationIds)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, conversationId.ToString());
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining user conversations");
        }
    }

    private async Task UpdateUnreadCounts(Guid conversationId, Guid senderId)
    {
        try
        {
            // TODO: Get conversation participants and increment their unread counts
            // This would require querying the conversation repository
            // For now, clients will query unread counts periodically
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating unread counts");
        }
    }

    #endregion
}

/// <summary>
/// Request model for sending messages via SignalR
/// </summary>
public class SendMessageRequest
{
    public Guid ConversationId { get; set; }
    public string Content { get; set; } = string.Empty;
    public Guid? ReplyToMessageId { get; set; }
}

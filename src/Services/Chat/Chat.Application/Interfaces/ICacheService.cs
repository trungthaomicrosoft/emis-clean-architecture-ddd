using Chat.Domain.Aggregates;
using Chat.Domain.Entities;

namespace Chat.Application.Interfaces;

/// <summary>
/// Interface for distributed cache service (Redis implementation in Infrastructure layer)
/// Provides caching for conversations, online users, unread counts, typing indicators
/// </summary>
public interface ICacheService
{
    #region Conversation Cache

    Task<Conversation?> GetConversationAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default);

    Task SetConversationAsync(
        Conversation conversation,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default);

    Task RemoveConversationAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default);

    Task<List<Guid>?> GetUserConversationsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task SetUserConversationsAsync(
        Guid userId,
        List<Guid> conversationIds,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default);

    Task RemoveUserConversationsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    #endregion

    #region Unread Count Cache

    Task<int?> GetUnreadCountAsync(
        Guid userId,
        Guid conversationId,
        CancellationToken cancellationToken = default);

    Task SetUnreadCountAsync(
        Guid userId,
        Guid conversationId,
        int count,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default);

    Task IncrementUnreadCountAsync(
        Guid userId,
        Guid conversationId,
        CancellationToken cancellationToken = default);

    Task ResetUnreadCountAsync(
        Guid userId,
        Guid conversationId,
        CancellationToken cancellationToken = default);

    Task RemoveUnreadCountAsync(
        Guid userId,
        Guid conversationId,
        CancellationToken cancellationToken = default);

    #endregion

    #region Online Users Cache

    Task<List<Guid>?> GetOnlineUsersAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task AddOnlineUserAsync(
        Guid tenantId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task RemoveOnlineUserAsync(
        Guid tenantId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<bool> IsUserOnlineAsync(
        Guid tenantId,
        Guid userId,
        CancellationToken cancellationToken = default);

    #endregion

    #region Typing Indicator Cache

    Task<List<Guid>?> GetTypingUsersAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default);

    Task AddTypingUserAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task RemoveTypingUserAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken cancellationToken = default);

    #endregion

    #region Generic Cache Operations

    Task<T?> GetAsync<T>(
        string key,
        CancellationToken cancellationToken = default) where T : class;

    Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default) where T : class;

    Task RemoveAsync(
        string key,
        CancellationToken cancellationToken = default);

    Task RemoveByPatternAsync(
        string pattern,
        CancellationToken cancellationToken = default);

    #endregion
}

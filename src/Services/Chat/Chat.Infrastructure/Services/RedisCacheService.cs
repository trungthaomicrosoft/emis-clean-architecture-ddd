using Chat.Application.Interfaces;
using Chat.Domain.Aggregates;
using Chat.Domain.Entities;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace Chat.Infrastructure.Services;

/// <summary>
/// Redis implementation of ICacheService
/// Provides distributed caching for conversations, online users, unread counts
/// </summary>
public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly TimeSpan _defaultExpiration;
    private readonly JsonSerializerOptions _jsonOptions;

    // Cache key prefixes
    private const string ConversationPrefix = "conversation:";
    private const string UserConversationsPrefix = "user_conversations:";
    private const string UnreadCountPrefix = "unread_count:";
    private const string OnlineUsersPrefix = "online_users:";
    private const string TypingUsersPrefix = "typing:";

    public RedisCacheService(IDistributedCache cache)
    {
        _cache = cache;
        _defaultExpiration = TimeSpan.FromMinutes(60); // 1 hour default
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    #region Conversation Cache

    public async Task<Conversation?> GetConversationAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        var key = $"{ConversationPrefix}{conversationId}";
        var json = await _cache.GetStringAsync(key, cancellationToken);
        
        if (string.IsNullOrEmpty(json))
            return null;

        return JsonSerializer.Deserialize<Conversation>(json, _jsonOptions);
    }

    public async Task SetConversationAsync(
        Conversation conversation,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        var key = $"{ConversationPrefix}{conversation.ConversationId}";
        var json = JsonSerializer.Serialize(conversation, _jsonOptions);
        
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration ?? _defaultExpiration
        };

        await _cache.SetStringAsync(key, json, options, cancellationToken);
    }

    public async Task RemoveConversationAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        var key = $"{ConversationPrefix}{conversationId}";
        await _cache.RemoveAsync(key, cancellationToken);
    }

    public async Task<List<Guid>?> GetUserConversationsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var key = $"{UserConversationsPrefix}{userId}";
        var json = await _cache.GetStringAsync(key, cancellationToken);
        
        if (string.IsNullOrEmpty(json))
            return null;

        return JsonSerializer.Deserialize<List<Guid>>(json, _jsonOptions);
    }

    public async Task SetUserConversationsAsync(
        Guid userId,
        List<Guid> conversationIds,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        var key = $"{UserConversationsPrefix}{userId}";
        var json = JsonSerializer.Serialize(conversationIds, _jsonOptions);
        
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration ?? _defaultExpiration
        };

        await _cache.SetStringAsync(key, json, options, cancellationToken);
    }

    public async Task RemoveUserConversationsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var key = $"{UserConversationsPrefix}{userId}";
        await _cache.RemoveAsync(key, cancellationToken);
    }

    #endregion

    #region Unread Count Cache

    public async Task<int?> GetUnreadCountAsync(
        Guid userId,
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        var key = $"{UnreadCountPrefix}{userId}:{conversationId}";
        var value = await _cache.GetStringAsync(key, cancellationToken);
        
        if (string.IsNullOrEmpty(value))
            return null;

        return int.Parse(value);
    }

    public async Task SetUnreadCountAsync(
        Guid userId,
        Guid conversationId,
        int count,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        var key = $"{UnreadCountPrefix}{userId}:{conversationId}";
        
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromHours(24) // Longer expiration for unread counts
        };

        await _cache.SetStringAsync(key, count.ToString(), options, cancellationToken);
    }

    public async Task IncrementUnreadCountAsync(
        Guid userId,
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        // Get current count
        var currentCount = await GetUnreadCountAsync(userId, conversationId, cancellationToken) ?? 0;
        
        // Increment and set
        await SetUnreadCountAsync(userId, conversationId, currentCount + 1, null, cancellationToken);
    }

    public async Task ResetUnreadCountAsync(
        Guid userId,
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        await SetUnreadCountAsync(userId, conversationId, 0, null, cancellationToken);
    }

    public async Task RemoveUnreadCountAsync(
        Guid userId,
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        var key = $"{UnreadCountPrefix}{userId}:{conversationId}";
        await _cache.RemoveAsync(key, cancellationToken);
    }

    #endregion

    #region Online Users Cache

    public async Task<List<Guid>?> GetOnlineUsersAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var key = $"{OnlineUsersPrefix}{tenantId}";
        var json = await _cache.GetStringAsync(key, cancellationToken);
        
        if (string.IsNullOrEmpty(json))
            return null;

        return JsonSerializer.Deserialize<List<Guid>>(json, _jsonOptions);
    }

    public async Task AddOnlineUserAsync(
        Guid tenantId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var onlineUsers = await GetOnlineUsersAsync(tenantId, cancellationToken) ?? new List<Guid>();
        
        if (!onlineUsers.Contains(userId))
        {
            onlineUsers.Add(userId);
            await SetOnlineUsersAsync(tenantId, onlineUsers, cancellationToken);
        }
    }

    public async Task RemoveOnlineUserAsync(
        Guid tenantId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var onlineUsers = await GetOnlineUsersAsync(tenantId, cancellationToken);
        
        if (onlineUsers != null && onlineUsers.Contains(userId))
        {
            onlineUsers.Remove(userId);
            await SetOnlineUsersAsync(tenantId, onlineUsers, cancellationToken);
        }
    }

    public async Task<bool> IsUserOnlineAsync(
        Guid tenantId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var onlineUsers = await GetOnlineUsersAsync(tenantId, cancellationToken);
        return onlineUsers?.Contains(userId) ?? false;
    }

    private async Task SetOnlineUsersAsync(
        Guid tenantId,
        List<Guid> userIds,
        CancellationToken cancellationToken = default)
    {
        var key = $"{OnlineUsersPrefix}{tenantId}";
        var json = JsonSerializer.Serialize(userIds, _jsonOptions);
        
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30) // Online status expires after 30 minutes
        };

        await _cache.SetStringAsync(key, json, options, cancellationToken);
    }

    #endregion

    #region Typing Indicator Cache

    public async Task<List<Guid>?> GetTypingUsersAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        var key = $"{TypingUsersPrefix}{conversationId}";
        var json = await _cache.GetStringAsync(key, cancellationToken);
        
        if (string.IsNullOrEmpty(json))
            return null;

        return JsonSerializer.Deserialize<List<Guid>>(json, _jsonOptions);
    }

    public async Task AddTypingUserAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var typingUsers = await GetTypingUsersAsync(conversationId, cancellationToken) ?? new List<Guid>();
        
        if (!typingUsers.Contains(userId))
        {
            typingUsers.Add(userId);
            await SetTypingUsersAsync(conversationId, typingUsers, cancellationToken);
        }
    }

    public async Task RemoveTypingUserAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var typingUsers = await GetTypingUsersAsync(conversationId, cancellationToken);
        
        if (typingUsers != null && typingUsers.Contains(userId))
        {
            typingUsers.Remove(userId);
            await SetTypingUsersAsync(conversationId, typingUsers, cancellationToken);
        }
    }

    private async Task SetTypingUsersAsync(
        Guid conversationId,
        List<Guid> userIds,
        CancellationToken cancellationToken = default)
    {
        var key = $"{TypingUsersPrefix}{conversationId}";
        var json = JsonSerializer.Serialize(userIds, _jsonOptions);
        
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(5) // Typing indicator expires after 5 seconds
        };

        await _cache.SetStringAsync(key, json, options, cancellationToken);
    }

    #endregion

    #region Generic Cache Operations

    public async Task<T?> GetAsync<T>(
        string key,
        CancellationToken cancellationToken = default) where T : class
    {
        var json = await _cache.GetStringAsync(key, cancellationToken);
        
        if (string.IsNullOrEmpty(json))
            return null;

        return JsonSerializer.Deserialize<T>(json, _jsonOptions);
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default) where T : class
    {
        var json = JsonSerializer.Serialize(value, _jsonOptions);
        
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration ?? _defaultExpiration
        };

        await _cache.SetStringAsync(key, json, options, cancellationToken);
    }

    public async Task RemoveAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        await _cache.RemoveAsync(key, cancellationToken);
    }

    public async Task RemoveByPatternAsync(
        string pattern,
        CancellationToken cancellationToken = default)
    {
        // Note: Pattern-based deletion is not natively supported by IDistributedCache
        // This would require a custom implementation using StackExchange.Redis directly
        // For now, we'll leave this as a TODO
        throw new NotImplementedException(
            "Pattern-based cache removal requires direct StackExchange.Redis access. " +
            "Consider implementing a custom Redis service for this functionality.");
    }

    #endregion
}

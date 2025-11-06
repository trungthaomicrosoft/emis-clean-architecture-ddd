using Chat.Domain.Entities;
using Chat.Domain.Enums;

namespace Chat.Domain.Repositories;

/// <summary>
/// Repository interface for Message entity
/// Following DDD best practices: encapsulated queries, NO IQueryable exposure
/// </summary>
public interface IMessageRepository
{
    /// <summary>
    /// Get message by ID
    /// </summary>
    Task<Message?> GetByIdAsync(Guid messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get messages for a conversation with cursor-based pagination
    /// More efficient than skip/take for large datasets
    /// </summary>
    Task<(IEnumerable<Message> Items, bool HasMore)> GetMessagesAsync(
        Guid conversationId,
        DateTime? beforeTimestamp,
        int pageSize = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get pinned messages in a conversation
    /// </summary>
    Task<IEnumerable<Message>> GetPinnedMessagesAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Search messages in a conversation (full-text search)
    /// Phase 1: MongoDB text search
    /// Phase 2: Can be replaced with Elasticsearch
    /// </summary>
    Task<(IEnumerable<Message> Items, int TotalCount)> SearchMessagesAsync(
        Guid conversationId,
        string searchTerm,
        MessageType? filterType,
        DateTime? fromDate,
        DateTime? toDate,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get messages by type (e.g., all images, all files)
    /// Useful for media gallery view
    /// </summary>
    Task<(IEnumerable<Message> Items, int TotalCount)> GetMessagesByTypeAsync(
        Guid conversationId,
        MessageType type,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get unread messages count for a user in a conversation
    /// </summary>
    Task<int> GetUnreadMessagesCountAsync(
        Guid conversationId,
        Guid userId,
        DateTime? lastReadAt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get messages that mention a specific user
    /// </summary>
    Task<IEnumerable<Message>> GetMessagesMentioningUserAsync(
        Guid conversationId,
        Guid userId,
        int pageSize = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all messages in a conversation (for export/backup)
    /// Use with caution - may return large datasets
    /// </summary>
    Task<IEnumerable<Message>> GetAllMessagesAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get messages sent by a user (for admin/audit purposes)
    /// </summary>
    Task<(IEnumerable<Message> Items, int TotalCount)> GetMessagesBySenderAsync(
        Guid senderId,
        Guid tenantId,
        DateTime? fromDate,
        DateTime? toDate,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if message exists
    /// </summary>
    Task<bool> ExistsAsync(Guid messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add new message
    /// </summary>
    Task AddAsync(Message message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update existing message
    /// </summary>
    Task UpdateAsync(Message message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete message (hard delete - for cleanup jobs)
    /// Messages are soft-deleted by default via Message.Delete()
    /// </summary>
    Task DeleteAsync(Guid messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk delete messages older than specified date (for cleanup)
    /// </summary>
    Task DeleteOldMessagesAsync(
        DateTime olderThan,
        int batchSize = 1000,
        CancellationToken cancellationToken = default);
}

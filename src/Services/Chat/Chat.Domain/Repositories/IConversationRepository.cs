using Chat.Domain.Aggregates;
using Chat.Domain.Enums;

namespace Chat.Domain.Repositories;

/// <summary>
/// Repository interface for Conversation aggregate
/// Following DDD best practices: encapsulated queries, NO IQueryable exposure
/// </summary>
public interface IConversationRepository
{
    /// <summary>
    /// Get conversation by ID
    /// </summary>
    Task<Conversation?> GetByIdAsync(Guid conversationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all conversations for a user with pagination and filters
    /// </summary>
    Task<(IEnumerable<Conversation> Items, int TotalCount)> GetUserConversationsAsync(
        Guid userId,
        Guid tenantId,
        ConversationType? filterType,
        bool includeArchived,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Find existing OneToOne conversation between two users
    /// Returns null if not found
    /// </summary>
    Task<Conversation?> FindOneToOneConversationAsync(
        Guid user1Id,
        Guid user2Id,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Find StudentGroup conversation by studentId
    /// Returns null if not found
    /// </summary>
    Task<Conversation?> FindStudentGroupByStudentIdAsync(
        Guid studentId,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Find ClassGroup conversation by classId
    /// Returns null if not found
    /// </summary>
    Task<Conversation?> FindClassGroupByClassIdAsync(
        Guid classId,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Search conversations by name
    /// </summary>
    Task<(IEnumerable<Conversation> Items, int TotalCount)> SearchConversationsByNameAsync(
        Guid userId,
        Guid tenantId,
        string searchTerm,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get unread conversations count for a user
    /// </summary>
    Task<int> GetUnreadConversationsCountAsync(
        Guid userId,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all conversations for a class (for teachers)
    /// </summary>
    Task<IEnumerable<Conversation>> GetConversationsByClassIdAsync(
        Guid classId,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if conversation exists
    /// </summary>
    Task<bool> ExistsAsync(Guid conversationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add new conversation
    /// </summary>
    Task AddAsync(Conversation conversation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update existing conversation
    /// </summary>
    Task UpdateAsync(Conversation conversation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete conversation (hard delete - use with caution)
    /// </summary>
    Task DeleteAsync(Guid conversationId, CancellationToken cancellationToken = default);
}

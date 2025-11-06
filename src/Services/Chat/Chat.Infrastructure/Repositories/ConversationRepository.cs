using Chat.Domain.Aggregates;
using Chat.Domain.Enums;
using Chat.Domain.Repositories;
using Chat.Infrastructure.Persistence;
using MongoDB.Driver;

namespace Chat.Infrastructure.Repositories;

/// <summary>
/// MongoDB implementation of IConversationRepository
/// Follows DDD best practices: encapsulated queries, NO IQueryable exposure
/// </summary>
public class ConversationRepository : IConversationRepository
{
    private readonly ChatDbContext _context;
    private readonly IMongoCollection<Conversation> _conversations;

    public ConversationRepository(ChatDbContext context)
    {
        _context = context;
        _conversations = context.Conversations;
    }

    public async Task<Conversation?> GetByIdAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        var filter = Builders<Conversation>.Filter.Eq(c => c.ConversationId, conversationId);
        return await _conversations.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Conversation?> FindOneToOneConversationAsync(
        Guid tenantId,
        Guid user1Id,
        Guid user2Id,
        CancellationToken cancellationToken = default)
    {
        // Find OneToOne conversation where both users are participants
        var filter = Builders<Conversation>.Filter.And(
            Builders<Conversation>.Filter.Eq(c => c.TenantId, tenantId),
            Builders<Conversation>.Filter.Eq(c => c.Type, ConversationType.OneToOne),
            Builders<Conversation>.Filter.ElemMatch(c => c.Participants, p => p.UserId == user1Id),
            Builders<Conversation>.Filter.ElemMatch(c => c.Participants, p => p.UserId == user2Id)
        );

        return await _conversations.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Conversation?> FindStudentGroupByStudentIdAsync(
        Guid tenantId,
        Guid studentId,
        CancellationToken cancellationToken = default)
    {
        var filter = Builders<Conversation>.Filter.And(
            Builders<Conversation>.Filter.Eq(c => c.TenantId, tenantId),
            Builders<Conversation>.Filter.Eq(c => c.Type, ConversationType.StudentGroup),
            Builders<Conversation>.Filter.Eq("Metadata.StudentId", studentId)
        );

        return await _conversations.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Conversation?> FindClassGroupByClassIdAsync(
        Guid tenantId,
        Guid classId,
        CancellationToken cancellationToken = default)
    {
        var filter = Builders<Conversation>.Filter.And(
            Builders<Conversation>.Filter.Eq(c => c.TenantId, tenantId),
            Builders<Conversation>.Filter.Eq(c => c.Type, ConversationType.ClassGroup),
            Builders<Conversation>.Filter.Eq("Metadata.ClassId", classId)
        );

        return await _conversations.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<(IEnumerable<Conversation> Items, int TotalCount)> GetUserConversationsAsync(
        Guid userId,
        Guid tenantId,
        ConversationType? filterType,
        bool includeArchived,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        // Build filter
        var filterBuilder = Builders<Conversation>.Filter;
        var filters = new List<FilterDefinition<Conversation>>
        {
            filterBuilder.Eq(c => c.TenantId, tenantId),
            filterBuilder.ElemMatch(c => c.Participants, p => p.UserId == userId)
        };

        if (filterType.HasValue)
        {
            filters.Add(filterBuilder.Eq(c => c.Type, filterType.Value));
        }

        if (!includeArchived)
        {
            filters.Add(filterBuilder.Eq(c => c.IsActive, true));
        }

        var filter = filterBuilder.And(filters);

        // Get total count
        var totalCount = await _conversations.CountDocumentsAsync(filter, cancellationToken: cancellationToken);

        // Get paginated results, sorted by UpdatedAt (most recent first)
        var conversations = await _conversations
            .Find(filter)
            .SortByDescending(c => c.UpdatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);

        return (conversations, (int)totalCount);
    }

    public async Task<(IEnumerable<Conversation> Items, int TotalCount)> SearchConversationsByNameAsync(
        Guid userId,
        Guid tenantId,
        string searchTerm,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        // Build filter
        var filterBuilder = Builders<Conversation>.Filter;
        var filters = new List<FilterDefinition<Conversation>>
        {
            filterBuilder.Eq(c => c.TenantId, tenantId),
            filterBuilder.ElemMatch(c => c.Participants, p => p.UserId == userId),
            filterBuilder.Regex(c => c.Name, new MongoDB.Bson.BsonRegularExpression(searchTerm, "i")) // Case-insensitive search
        };

        var filter = filterBuilder.And(filters);

        // Get total count
        var totalCount = await _conversations.CountDocumentsAsync(filter, cancellationToken: cancellationToken);

        // Get paginated results
        var conversations = await _conversations
            .Find(filter)
            .SortByDescending(c => c.UpdatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);

        return (conversations, (int)totalCount);
    }

    public async Task<int> GetUnreadConversationsCountAsync(
        Guid userId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        // Count conversations where user has unread messages
        // A conversation is unread if the participant's UnreadCount > 0
        var filter = Builders<Conversation>.Filter.And(
            Builders<Conversation>.Filter.Eq(c => c.TenantId, tenantId),
            Builders<Conversation>.Filter.ElemMatch(c => c.Participants, 
                p => p.UserId == userId && p.UnreadCount > 0)
        );

        var count = await _conversations.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
        return (int)count;
    }

    public async Task<IEnumerable<Conversation>> GetConversationsByTypeAsync(
        Guid tenantId,
        ConversationType type,
        CancellationToken cancellationToken = default)
    {
        var filter = Builders<Conversation>.Filter.And(
            Builders<Conversation>.Filter.Eq(c => c.TenantId, tenantId),
            Builders<Conversation>.Filter.Eq(c => c.Type, type)
        );

        return await _conversations.Find(filter).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Conversation>> GetConversationsForStudentAsync(
        Guid tenantId,
        Guid studentId,
        CancellationToken cancellationToken = default)
    {
        // Get all conversations related to a student
        // This includes StudentGroup and ClassGroup where the student's parents are participants
        var filter = Builders<Conversation>.Filter.And(
            Builders<Conversation>.Filter.Eq(c => c.TenantId, tenantId),
            Builders<Conversation>.Filter.Or(
                Builders<Conversation>.Filter.Eq("Metadata.StudentId", studentId),
                Builders<Conversation>.Filter.And(
                    Builders<Conversation>.Filter.Eq(c => c.Type, ConversationType.ClassGroup),
                    Builders<Conversation>.Filter.Eq("Metadata.ClassId", studentId) // Assuming classId matches
                )
            )
        );

        return await _conversations.Find(filter).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Conversation>> GetConversationsForClassAsync(
        Guid tenantId,
        Guid classId,
        CancellationToken cancellationToken = default)
    {
        var filter = Builders<Conversation>.Filter.And(
            Builders<Conversation>.Filter.Eq(c => c.TenantId, tenantId),
            Builders<Conversation>.Filter.Eq("Metadata.ClassId", classId)
        );

        return await _conversations.Find(filter).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Conversation>> GetConversationsByClassIdAsync(
        Guid classId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        // Same as GetConversationsForClassAsync but with different parameter order
        return await GetConversationsForClassAsync(tenantId, classId, cancellationToken);
    }

    public async Task<bool> ExistsAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        var filter = Builders<Conversation>.Filter.Eq(c => c.ConversationId, conversationId);
        var count = await _conversations.CountDocumentsAsync(filter, new CountOptions { Limit = 1 }, cancellationToken);
        return count > 0;
    }

    public async Task AddAsync(
        Conversation conversation,
        CancellationToken cancellationToken = default)
    {
        await _conversations.InsertOneAsync(conversation, cancellationToken: cancellationToken);
    }

    public async Task UpdateAsync(
        Conversation conversation,
        CancellationToken cancellationToken = default)
    {
        var filter = Builders<Conversation>.Filter.Eq(c => c.ConversationId, conversation.ConversationId);
        await _conversations.ReplaceOneAsync(filter, conversation, cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        var filter = Builders<Conversation>.Filter.Eq(c => c.ConversationId, conversationId);
        await _conversations.DeleteOneAsync(filter, cancellationToken);
    }

    public async Task<IEnumerable<Conversation>> GetActiveConversationsForCacheAsync(
        Guid tenantId,
        DateTime since,
        CancellationToken cancellationToken = default)
    {
        // Get conversations updated since a certain time (for cache warming)
        var filter = Builders<Conversation>.Filter.And(
            Builders<Conversation>.Filter.Eq(c => c.TenantId, tenantId),
            Builders<Conversation>.Filter.Eq(c => c.IsActive, true),
            Builders<Conversation>.Filter.Gte(c => c.UpdatedAt, since)
        );

        return await _conversations
            .Find(filter)
            .Limit(1000) // Limit to prevent overload
            .ToListAsync(cancellationToken);
    }

    public async Task BulkUpdateParticipantsAsync(
        List<(Guid ConversationId, Guid UserId, int UnreadCount)> updates,
        CancellationToken cancellationToken = default)
    {
        // Bulk update for marking messages as read across multiple conversations
        var bulkOps = new List<WriteModel<Conversation>>();

        foreach (var (conversationId, userId, unreadCount) in updates)
        {
            var filter = Builders<Conversation>.Filter.And(
                Builders<Conversation>.Filter.Eq(c => c.ConversationId, conversationId),
                Builders<Conversation>.Filter.ElemMatch(c => c.Participants, p => p.UserId == userId)
            );

            var update = Builders<Conversation>.Update
                .Set("Participants.$.UnreadCount", unreadCount)
                .Set("Participants.$.LastReadAt", DateTime.UtcNow);

            bulkOps.Add(new UpdateOneModel<Conversation>(filter, update));
        }

        if (bulkOps.Any())
        {
            await _conversations.BulkWriteAsync(bulkOps, cancellationToken: cancellationToken);
        }
    }

    public async Task<long> GetTotalConversationsCountAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var filter = Builders<Conversation>.Filter.Eq(c => c.TenantId, tenantId);
        return await _conversations.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
    }

    public async Task<Dictionary<ConversationType, long>> GetConversationsCountByTypeAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        // Aggregate to count conversations by type
        var pipeline = _conversations.Aggregate()
            .Match(Builders<Conversation>.Filter.Eq(c => c.TenantId, tenantId))
            .Group(c => c.Type, g => new { Type = g.Key, Count = g.Count() });

        var results = await pipeline.ToListAsync(cancellationToken);

        return results.ToDictionary(r => r.Type, r => (long)r.Count);
    }
}

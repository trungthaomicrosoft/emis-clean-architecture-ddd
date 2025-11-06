using Chat.Domain.Entities;
using Chat.Domain.Enums;
using Chat.Domain.Repositories;
using Chat.Infrastructure.Persistence;
using MongoDB.Driver;

namespace Chat.Infrastructure.Repositories;

/// <summary>
/// MongoDB implementation of IMessageRepository
/// Implements cursor-based pagination for scalability
/// </summary>
public class MessageRepository : IMessageRepository
{
    private readonly ChatDbContext _context;
    private readonly IMongoCollection<Message> _messages;

    public MessageRepository(ChatDbContext context)
    {
        _context = context;
        _messages = context.Messages;
    }

    public async Task<Message?> GetByIdAsync(
        Guid messageId,
        CancellationToken cancellationToken = default)
    {
        var filter = Builders<Message>.Filter.Eq(m => m.MessageId, messageId);
        return await _messages.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<(IEnumerable<Message> Items, bool HasMore)> GetMessagesAsync(
        Guid conversationId,
        DateTime? beforeTimestamp,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        // Cursor-based pagination using SentAt timestamp
        var filterBuilder = Builders<Message>.Filter;
        var filters = new List<FilterDefinition<Message>>
        {
            filterBuilder.Eq(m => m.ConversationId, conversationId),
            filterBuilder.Eq(m => m.IsDeleted, false) // Don't show deleted messages
        };

        if (beforeTimestamp.HasValue)
        {
            filters.Add(filterBuilder.Lt(m => m.SentAt, beforeTimestamp.Value));
        }

        var filter = filterBuilder.And(filters);

        // Fetch pageSize + 1 to check if there are more messages
        var messages = await _messages
            .Find(filter)
            .SortByDescending(m => m.SentAt)
            .Limit(pageSize + 1)
            .ToListAsync(cancellationToken);

        var hasMore = messages.Count > pageSize;
        if (hasMore)
        {
            messages = messages.Take(pageSize).ToList();
        }

        return (messages, hasMore);
    }

    public async Task<IEnumerable<Message>> GetPinnedMessagesAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        var filter = Builders<Message>.Filter.And(
            Builders<Message>.Filter.Eq(m => m.ConversationId, conversationId),
            Builders<Message>.Filter.Eq(m => m.IsPinned, true),
            Builders<Message>.Filter.Eq(m => m.IsDeleted, false)
        );

        return await _messages
            .Find(filter)
            .SortByDescending(m => m.PinnedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IEnumerable<Message> Items, int TotalCount)> SearchMessagesAsync(
        Guid conversationId,
        string searchTerm,
        MessageType? filterType,
        DateTime? fromDate,
        DateTime? toDate,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        // Build filter
        var filterBuilder = Builders<Message>.Filter;
        var filters = new List<FilterDefinition<Message>>
        {
            filterBuilder.Eq(m => m.ConversationId, conversationId),
            filterBuilder.Eq(m => m.IsDeleted, false),
            filterBuilder.Text(searchTerm) // MongoDB text search on Content field
        };

        if (filterType.HasValue)
        {
            filters.Add(filterBuilder.Eq(m => m.Type, filterType.Value));
        }

        if (fromDate.HasValue)
        {
            filters.Add(filterBuilder.Gte(m => m.SentAt, fromDate.Value));
        }

        if (toDate.HasValue)
        {
            filters.Add(filterBuilder.Lte(m => m.SentAt, toDate.Value));
        }

        var filter = filterBuilder.And(filters);

        // Get total count
        var totalCount = await _messages.CountDocumentsAsync(filter, cancellationToken: cancellationToken);

        // Get paginated results
        var messages = await _messages
            .Find(filter)
            .SortByDescending(m => m.SentAt)
            .Skip((pageNumber - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);

        return (messages, (int)totalCount);
    }

    public async Task<(IEnumerable<Message> Items, int TotalCount)> GetMessagesByTypeAsync(
        Guid conversationId,
        MessageType type,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var filter = Builders<Message>.Filter.And(
            Builders<Message>.Filter.Eq(m => m.ConversationId, conversationId),
            Builders<Message>.Filter.Eq(m => m.Type, type),
            Builders<Message>.Filter.Eq(m => m.IsDeleted, false)
        );

        // Get total count
        var totalCount = await _messages.CountDocumentsAsync(filter, cancellationToken: cancellationToken);

        // Get paginated results
        var messages = await _messages
            .Find(filter)
            .SortByDescending(m => m.SentAt)
            .Skip((pageNumber - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);

        return (messages, (int)totalCount);
    }

    public async Task<int> GetUnreadMessagesCountAsync(
        Guid conversationId,
        Guid userId,
        DateTime? lastReadAt,
        CancellationToken cancellationToken = default)
    {
        var filterBuilder = Builders<Message>.Filter;
        var filters = new List<FilterDefinition<Message>>
        {
            filterBuilder.Eq(m => m.ConversationId, conversationId),
            filterBuilder.Ne(m => m.SenderId, userId), // Don't count own messages
            filterBuilder.Eq(m => m.IsDeleted, false)
        };

        if (lastReadAt.HasValue)
        {
            filters.Add(filterBuilder.Gt(m => m.SentAt, lastReadAt.Value));
        }

        var filter = filterBuilder.And(filters);
        var count = await _messages.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
        return (int)count;
    }

    public async Task<IEnumerable<Message>> GetMessagesMentioningUserAsync(
        Guid conversationId,
        Guid userId,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var filter = Builders<Message>.Filter.And(
            Builders<Message>.Filter.Eq(m => m.ConversationId, conversationId),
            Builders<Message>.Filter.ElemMatch(m => m.Mentions, mention => mention.UserId == userId),
            Builders<Message>.Filter.Eq(m => m.IsDeleted, false)
        );

        return await _messages
            .Find(filter)
            .SortByDescending(m => m.SentAt)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Message>> GetAllMessagesAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        var filter = Builders<Message>.Filter.Eq(m => m.ConversationId, conversationId);
        
        return await _messages
            .Find(filter)
            .SortBy(m => m.SentAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IEnumerable<Message> Items, int TotalCount)> GetMessagesBySenderAsync(
        Guid senderId,
        Guid tenantId,
        DateTime? fromDate,
        DateTime? toDate,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        // Build filter
        var filterBuilder = Builders<Message>.Filter;
        var filters = new List<FilterDefinition<Message>>
        {
            filterBuilder.Eq(m => m.SenderId, senderId)
        };

        if (fromDate.HasValue)
        {
            filters.Add(filterBuilder.Gte(m => m.SentAt, fromDate.Value));
        }

        if (toDate.HasValue)
        {
            filters.Add(filterBuilder.Lte(m => m.SentAt, toDate.Value));
        }

        var filter = filterBuilder.And(filters);

        // Get total count
        var totalCount = await _messages.CountDocumentsAsync(filter, cancellationToken: cancellationToken);

        // Get paginated results
        var messages = await _messages
            .Find(filter)
            .SortByDescending(m => m.SentAt)
            .Skip((pageNumber - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);

        return (messages, (int)totalCount);
    }

    public async Task<bool> ExistsAsync(
        Guid messageId,
        CancellationToken cancellationToken = default)
    {
        var filter = Builders<Message>.Filter.Eq(m => m.MessageId, messageId);
        var count = await _messages.CountDocumentsAsync(filter, new CountOptions { Limit = 1 }, cancellationToken);
        return count > 0;
    }

    public async Task AddAsync(
        Message message,
        CancellationToken cancellationToken = default)
    {
        await _messages.InsertOneAsync(message, cancellationToken: cancellationToken);
    }

    public async Task UpdateAsync(
        Message message,
        CancellationToken cancellationToken = default)
    {
        var filter = Builders<Message>.Filter.Eq(m => m.MessageId, message.MessageId);
        await _messages.ReplaceOneAsync(filter, message, cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(
        Guid messageId,
        CancellationToken cancellationToken = default)
    {
        // Hard delete
        var filter = Builders<Message>.Filter.Eq(m => m.MessageId, messageId);
        await _messages.DeleteOneAsync(filter, cancellationToken);
    }

    public async Task DeleteOldMessagesAsync(
        DateTime olderThan,
        int batchSize = 1000,
        CancellationToken cancellationToken = default)
    {
        // Cleanup job: delete messages older than specified date
        var filter = Builders<Message>.Filter.And(
            Builders<Message>.Filter.Lt(m => m.SentAt, olderThan),
            Builders<Message>.Filter.Eq(m => m.IsDeleted, true) // Only delete soft-deleted messages
        );

        var result = await _messages.DeleteManyAsync(filter, cancellationToken);
        
        // Log the result
        // TODO: Add logging here
    }

    public async Task<long> GetTotalMessagesCountAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        var filter = Builders<Message>.Filter.Eq(m => m.ConversationId, conversationId);
        return await _messages.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
    }

    public async Task<Dictionary<MessageType, long>> GetMessagesCountByTypeAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        // Aggregate to count messages by type
        var pipeline = _messages.Aggregate()
            .Match(Builders<Message>.Filter.Eq(m => m.ConversationId, conversationId))
            .Group(m => m.Type, g => new { Type = g.Key, Count = g.Count() });

        var results = await pipeline.ToListAsync(cancellationToken);

        return results.ToDictionary(r => r.Type, r => (long)r.Count);
    }

    public async Task<List<Message>> GetLatestMessagesAsync(
        Guid conversationId,
        int count = 10,
        CancellationToken cancellationToken = default)
    {
        var filter = Builders<Message>.Filter.And(
            Builders<Message>.Filter.Eq(m => m.ConversationId, conversationId),
            Builders<Message>.Filter.Eq(m => m.IsDeleted, false)
        );

        var messages = await _messages
            .Find(filter)
            .SortByDescending(m => m.SentAt)
            .Limit(count)
            .ToListAsync(cancellationToken);

        return messages;
    }

    public async Task BulkInsertAsync(
        List<Message> messages,
        CancellationToken cancellationToken = default)
    {
        if (messages.Any())
        {
            await _messages.InsertManyAsync(messages, cancellationToken: cancellationToken);
        }
    }

    public async Task<Dictionary<Guid, int>> GetUnreadCountsForConversationsAsync(
        List<Guid> conversationIds,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        // Get unread counts for multiple conversations at once (optimization)
        var result = new Dictionary<Guid, int>();

        foreach (var conversationId in conversationIds)
        {
            // This is a simplified version. In production, you might want to use aggregation
            var count = await GetUnreadMessagesCountAsync(conversationId, userId, null, cancellationToken);
            result[conversationId] = count;
        }

        return result;
    }
}

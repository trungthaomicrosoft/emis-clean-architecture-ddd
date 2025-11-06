using Chat.Domain.Aggregates;
using Chat.Domain.Entities;
using Chat.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Chat.Infrastructure.Persistence;

/// <summary>
/// MongoDB Database Context for Chat Service
/// Provides access to MongoDB collections
/// </summary>
public class ChatDbContext
{
    private readonly IMongoDatabase _database;
    private readonly MongoDbSettings _settings;

    public ChatDbContext(IOptions<MongoDbSettings> settings)
    {
        _settings = settings.Value;
        
        var client = new MongoClient(_settings.ConnectionString);
        _database = client.GetDatabase(_settings.DatabaseName);
    }

    /// <summary>
    /// Conversations collection
    /// </summary>
    public IMongoCollection<Conversation> Conversations =>
        _database.GetCollection<Conversation>(_settings.ConversationsCollection);

    /// <summary>
    /// Messages collection
    /// </summary>
    public IMongoCollection<Message> Messages =>
        _database.GetCollection<Message>(_settings.MessagesCollection);

    /// <summary>
    /// Create indexes for performance optimization
    /// Following design from CHAT_SERVICE_DESIGN.md
    /// </summary>
    public async Task EnsureIndexesAsync()
    {
        // Conversation Indexes
        var conversationIndexes = Conversations.Indexes;
        
        // 1. TenantId + ConversationId (for sharding)
        await conversationIndexes.CreateOneAsync(new CreateIndexModel<Conversation>(
            Builders<Conversation>.IndexKeys
                .Ascending(c => c.TenantId)
                .Ascending(c => c.ConversationId),
            new CreateIndexOptions { Name = "idx_tenant_conversation" }));

        // 2. TenantId + Participants.UserId (for user's conversations)
        await conversationIndexes.CreateOneAsync(new CreateIndexModel<Conversation>(
            Builders<Conversation>.IndexKeys
                .Ascending(c => c.TenantId)
                .Ascending("Participants.UserId"),
            new CreateIndexOptions { Name = "idx_tenant_participant" }));

        // 3. Type + TenantId (for filtering by type)
        await conversationIndexes.CreateOneAsync(new CreateIndexModel<Conversation>(
            Builders<Conversation>.IndexKeys
                .Ascending(c => c.Type)
                .Ascending(c => c.TenantId),
            new CreateIndexOptions { Name = "idx_type_tenant" }));

        // 4. StudentId in metadata (for student groups)
        await conversationIndexes.CreateOneAsync(new CreateIndexModel<Conversation>(
            Builders<Conversation>.IndexKeys
                .Ascending("Metadata.StudentId")
                .Ascending(c => c.TenantId),
            new CreateIndexOptions { 
                Name = "idx_student_tenant",
                Sparse = true // Only for documents with StudentId
            }));

        // 5. ClassId in metadata (for class groups)
        await conversationIndexes.CreateOneAsync(new CreateIndexModel<Conversation>(
            Builders<Conversation>.IndexKeys
                .Ascending("Metadata.ClassId")
                .Ascending(c => c.TenantId),
            new CreateIndexOptions { 
                Name = "idx_class_tenant",
                Sparse = true
            }));

        // 6. UpdatedAt (for sorting by recent activity)
        await conversationIndexes.CreateOneAsync(new CreateIndexModel<Conversation>(
            Builders<Conversation>.IndexKeys
                .Descending(c => c.UpdatedAt),
            new CreateIndexOptions { Name = "idx_updated_at" }));

        // Message Indexes
        var messageIndexes = Messages.Indexes;

        // 1. ConversationId + SentAt (for cursor pagination)
        await messageIndexes.CreateOneAsync(new CreateIndexModel<Message>(
            Builders<Message>.IndexKeys
                .Ascending(m => m.ConversationId)
                .Descending(m => m.SentAt),
            new CreateIndexOptions { Name = "idx_conversation_sent" }));

        // 2. MessageId (unique identifier)
        await messageIndexes.CreateOneAsync(new CreateIndexModel<Message>(
            Builders<Message>.IndexKeys
                .Ascending(m => m.MessageId),
            new CreateIndexOptions { 
                Name = "idx_message_id",
                Unique = true 
            }));

        // 3. SenderId + SentAt (for user's messages)
        await messageIndexes.CreateOneAsync(new CreateIndexModel<Message>(
            Builders<Message>.IndexKeys
                .Ascending(m => m.SenderId)
                .Descending(m => m.SentAt),
            new CreateIndexOptions { Name = "idx_sender_sent" }));

        // 4. Type + ConversationId (for media gallery)
        await messageIndexes.CreateOneAsync(new CreateIndexModel<Message>(
            Builders<Message>.IndexKeys
                .Ascending(m => m.Type)
                .Ascending(m => m.ConversationId),
            new CreateIndexOptions { Name = "idx_type_conversation" }));

        // 5. IsPinned + ConversationId (for pinned messages)
        await messageIndexes.CreateOneAsync(new CreateIndexModel<Message>(
            Builders<Message>.IndexKeys
                .Ascending(m => m.IsPinned)
                .Ascending(m => m.ConversationId),
            new CreateIndexOptions { 
                Name = "idx_pinned_conversation",
                Sparse = true
            }));

        // 6. Text index for content search (MongoDB full-text search)
        await messageIndexes.CreateOneAsync(new CreateIndexModel<Message>(
            Builders<Message>.IndexKeys.Text(m => m.Content),
            new CreateIndexOptions { Name = "idx_content_text" }));

        // 7. Mentions.UserId (for finding mentions)
        await messageIndexes.CreateOneAsync(new CreateIndexModel<Message>(
            Builders<Message>.IndexKeys
                .Ascending("Mentions.UserId"),
            new CreateIndexOptions { 
                Name = "idx_mentions_user",
                Sparse = true
            }));
    }
}

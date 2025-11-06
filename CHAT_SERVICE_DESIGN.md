# Chat Service - Detailed Design Document

## 🎯 Overview

Chat Service là một real-time messaging platform cho phép giao tiếp giữa phụ huynh và giáo viên trong hệ thống EMIS. Service được thiết kế để xử lý 4M concurrent users với khả năng scale horizontally.

## 📋 Requirements Summary

### Functional Requirements

#### 1. Conversation Types
- **OneToOne**: Chat 1-1 giữa phụ huynh và giáo viên
- **StudentGroup**: Nhóm chat về 1 học sinh (phụ huynh + giáo viên chủ nhiệm)
- **ClassGroup**: Nhóm chat của 1 lớp (tất cả phụ huynh + giáo viên)
- **TeacherGroup**: Nhóm chat riêng cho giáo viên
- **AnnouncementChannel**: Kênh thông báo (chỉ admin/giáo viên gửi, phụ huynh đọc)

#### 2. Message Features
- ✅ Text messages
- ✅ Image messages (max 10MB)
- ✅ Video messages (max 25MB)
- ✅ Voice messages (max 10MB)
- ✅ File attachments (PDF, DOC, XLS, etc. - max 10MB)
- ✅ Reply/Quote messages
- ✅ Edit messages (within 15 minutes)
- ✅ Delete messages (soft delete)
- ✅ Forward messages
- ✅ Emoji reactions
- ✅ Mention/Tag users (@username)
- ✅ Pin messages

#### 3. Real-time Features
- ✅ Instant message delivery (SignalR)
- ✅ Typing indicators ("X is typing...")
- ✅ Online status ("Last seen X minutes ago")
- ✅ Read receipts (Delivered, Seen with timestamp)
- ✅ Delivery status tracking
- ✅ Multiple device connections
- ✅ Auto-reconnection (5 retries)
- ✅ Heartbeat (30s interval)

#### 4. Search & Discovery
- ✅ Search messages in conversation (full-text)
- ✅ Search conversations by name
- ✅ Filter messages by type (image, video, file)
- ✅ MongoDB text search (Phase 1)
- ✅ Elasticsearch (Phase 2 - designed for easy migration)

#### 5. Notifications
- ✅ Push notifications on new message (configurable)
- ✅ In-app badge with unread count
- ✅ Mention notifications
- ✅ Pinned message notifications

#### 6. Permissions & Security
- ✅ Parent can ONLY chat with teachers of their child's class
- ✅ Teacher can ONLY see conversations of their assigned classes
- ✅ Admin/Principal can view ALL conversations
- ✅ Role-based access control
- ❌ End-to-end encryption (Phase 2)

#### 7. Auto-group Creation
- ✅ ClassGroup created when class is created
- ✅ Parents auto-added to ClassGroup when student assigned to class
- ✅ Parents auto-added to StudentGroup when added to student
- ✅ Teachers auto-added to ClassGroup when assigned
- ❌ StudentGroup NOT auto-created (only when teacher initiates chat)

#### 8. Data Retention
- ✅ Messages: Permanent storage
- ✅ Files: 1-year retention (auto-cleanup)
- ✅ Deleted messages: Soft delete with audit trail

### Non-Functional Requirements

#### 1. Performance
- **Concurrent Users**: 4M active connections
- **Message Latency**: < 100ms for real-time delivery
- **Message Pagination**: 50 messages per page
- **API Response Time**: < 200ms (p95)
- **SignalR Reconnection**: < 3s

#### 2. Scalability
- **Horizontal Scaling**: Multiple API instances with Redis backplane
- **Database Sharding**: MongoDB sharding by TenantId
- **File Storage**: Distributed MinIO cluster
- **Load Balancing**: YARP API Gateway with round-robin

#### 3. Reliability
- **Uptime**: 99.9% availability
- **Message Delivery**: At-least-once guarantee
- **Auto-retry**: 5 attempts with exponential backoff
- **Circuit Breaker**: For external service calls

#### 4. Security
- **Authentication**: JWT tokens
- **Authorization**: Role-based + resource-based
- **File Scanning**: Virus/malware detection (optional)
- **Rate Limiting**: 100 messages/minute per user
- **Input Validation**: XSS/SQL injection prevention

---

## 🏗️ Architecture Design

### Clean Architecture Layers

```
┌─────────────────────────────────────────────────────────┐
│                      Chat.API                           │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐ │
│  │ Controllers  │  │  SignalR Hub │  │ Middlewares  │ │
│  └──────────────┘  └──────────────┘  └──────────────┘ │
└────────────────────────┬────────────────────────────────┘
                         │ IMediator
┌────────────────────────▼────────────────────────────────┐
│                 Chat.Application                        │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐ │
│  │  Commands    │  │   Queries    │  │  Validators  │ │
│  │  Handlers    │  │   Handlers   │  │  AutoMapper  │ │
│  └──────────────┘  └──────────────┘  └──────────────┘ │
└────────────────────────┬────────────────────────────────┘
                         │ IRepository
┌────────────────────────▼────────────────────────────────┐
│                Chat.Infrastructure                      │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐ │
│  │  MongoDB     │  │    Redis     │  │    MinIO     │ │
│  │ Repositories │  │    Cache     │  │File Storage  │ │
│  └──────────────┘  └──────────────┘  └──────────────┘ │
└────────────────────────┬────────────────────────────────┘
                         │
┌────────────────────────▼────────────────────────────────┐
│                   Chat.Domain                           │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐ │
│  │ Conversation │  │   Message    │  │    Value     │ │
│  │  (Aggregate) │  │   (Entity)   │  │   Objects    │ │
│  └──────────────┘  └──────────────┘  └──────────────┘ │
└─────────────────────────────────────────────────────────┘
```

---

## 📊 Domain Models (DDD)

### 1. Conversation (Aggregate Root)

**Responsibilities:**
- Manage conversation lifecycle
- Enforce participant rules
- Track conversation metadata
- Publish domain events

**Properties:**
```csharp
public class Conversation : TenantEntity, IAggregateRoot
{
    public Guid ConversationId { get; private set; }
    public ConversationType Type { get; private set; }
    public string Name { get; private set; } // For groups
    public ConversationMetadata Metadata { get; private set; } // StudentId, ClassId
    public List<Participant> Participants { get; private set; }
    public MessageSummary LastMessage { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public bool IsActive { get; private set; }
}
```

**Value Objects:**
- `ConversationMetadata`: StudentId, StudentName, ClassId, ClassName
- `MessageSummary`: MessageId, Content, SenderId, SenderName, SentAt

**Business Rules:**
- AnnouncementChannel must have at least 1 admin
- StudentGroup must have StudentId in metadata
- ClassGroup must have ClassId in metadata
- OneToOne conversation must have exactly 2 participants
- Parent can only be added to conversations of their child's class

**Domain Events:**
- `ConversationCreatedEvent`
- `ParticipantAddedEvent`
- `ParticipantRemovedEvent`
- `ConversationArchivedEvent`

### 2. Message (Entity)

**Responsibilities:**
- Store message content and metadata
- Track delivery and read status
- Manage reactions and pins
- Handle edit history

**Properties:**
```csharp
public class Message : Entity
{
    public Guid MessageId { get; private set; }
    public Guid ConversationId { get; private set; }
    public Guid SenderId { get; private set; }
    public string SenderName { get; private set; }
    public string Content { get; private set; }
    public MessageType Type { get; private set; }
    public List<Attachment> Attachments { get; private set; }
    public ReplyToMessage? ReplyTo { get; private set; }
    public List<Mention> Mentions { get; private set; }
    public List<Reaction> Reactions { get; private set; }
    public MessageStatus Status { get; private set; }
    public DateTime SentAt { get; private set; }
    public DateTime? EditedAt { get; private set; }
    public bool IsDeleted { get; private set; }
    public bool IsPinned { get; private set; }
    public Guid? PinnedBy { get; private set; }
    public List<ReadReceipt> ReadReceipts { get; private set; }
}
```

**Value Objects:**
- `Attachment`: FileName, FileType, FileSize, Url, ThumbnailUrl
- `ReplyToMessage`: MessageId, Content, SenderName
- `Mention`: UserId, UserName, StartIndex, Length
- `Reaction`: EmojiCode, UserId, UserName, ReactedAt
- `ReadReceipt`: UserId, ReadAt

**Business Rules:**
- Cannot edit after 15 minutes
- Cannot edit deleted messages
- Cannot pin in AnnouncementChannel (unless admin)
- File size limits: 10MB (general), 25MB (video)
- Content required for text messages
- Deleted messages retain sender info for audit

**Domain Events:**
- `MessageSentEvent`
- `MessageEditedEvent`
- `MessageDeletedEvent`
- `MessagePinnedEvent`
- `MessageUnpinnedEvent`
- `ReactionAddedEvent`
- `MessageReadEvent`

### 3. Participant (Value Object)

```csharp
public class Participant : ValueObject
{
    public Guid UserId { get; private set; }
    public string UserName { get; private set; }
    public ParticipantRole Role { get; private set; } // Member, Admin, ReadOnly
    public DateTime JoinedAt { get; private set; }
    public DateTime? LastReadAt { get; private set; }
    public int UnreadCount { get; private set; }
}
```

### 4. Enums

```csharp
public enum ConversationType
{
    OneToOne,
    StudentGroup,
    ClassGroup,
    TeacherGroup,
    AnnouncementChannel
}

public enum MessageType
{
    Text,
    Image,
    Video,
    Audio,
    File,
    System // For auto-generated messages
}

public enum MessageStatus
{
    Sent,
    Delivered,
    Read
}

public enum ParticipantRole
{
    Member,      // Can read and write
    Admin,       // Can manage group
    ReadOnly     // Can only read (for AnnouncementChannel)
}
```

---

## 🗄️ MongoDB Schema Design

### Conversations Collection

```javascript
{
  _id: ObjectId,
  conversationId: UUID,
  tenantId: UUID,
  type: "OneToOne|StudentGroup|ClassGroup|TeacherGroup|AnnouncementChannel",
  name: String,
  metadata: {
    studentId: UUID,
    studentName: String,
    classId: UUID,
    className: String
  },
  participants: [
    {
      userId: UUID,
      userName: String,
      role: "Member|Admin|ReadOnly",
      joinedAt: ISODate,
      lastReadAt: ISODate,
      unreadCount: Int
    }
  ],
  lastMessage: {
    messageId: UUID,
    content: String,
    senderId: UUID,
    senderName: String,
    sentAt: ISODate
  },
  createdAt: ISODate,
  updatedAt: ISODate,
  isActive: Boolean
}

// Indexes
db.conversations.createIndex({ tenantId: 1, "participants.userId": 1, updatedAt: -1 });
db.conversations.createIndex({ conversationId: 1 });
db.conversations.createIndex({ type: 1, "metadata.studentId": 1 });
db.conversations.createIndex({ type: 1, "metadata.classId": 1 });
db.conversations.createIndex({ tenantId: 1, isActive: 1 });
```

### Messages Collection

```javascript
{
  _id: ObjectId,
  messageId: UUID,
  conversationId: UUID,
  tenantId: UUID,
  senderId: UUID,
  senderName: String,
  content: String,
  type: "Text|Image|Video|Audio|File|System",
  attachments: [
    {
      attachmentId: UUID,
      fileName: String,
      fileType: String,
      fileSize: Long,
      url: String,
      thumbnailUrl: String
    }
  ],
  replyTo: {
    messageId: UUID,
    content: String,
    senderName: String
  },
  mentions: [
    {
      userId: UUID,
      userName: String,
      startIndex: Int,
      length: Int
    }
  ],
  reactions: [
    {
      emojiCode: String,
      userId: UUID,
      userName: String,
      reactedAt: ISODate
    }
  ],
  status: "Sent|Delivered|Read",
  sentAt: ISODate,
  editedAt: ISODate,
  isDeleted: Boolean,
  deletedAt: ISODate,
  isPinned: Boolean,
  pinnedBy: UUID,
  pinnedAt: ISODate,
  readReceipts: [
    {
      userId: UUID,
      readAt: ISODate
    }
  ]
}

// Indexes
db.messages.createIndex({ conversationId: 1, sentAt: -1 });
db.messages.createIndex({ messageId: 1 });
db.messages.createIndex({ tenantId: 1, senderId: 1 });
db.messages.createIndex({ conversationId: 1, isPinned: 1 });
db.messages.createIndex({ conversationId: 1, type: 1 }); // For filtering
// Text search index (Phase 1)
db.messages.createIndex({ content: "text", "senderName": "text" });
```

### Sharding Strategy

```javascript
// Shard key: { tenantId: 1, conversationId: 1 }
// Ensures all messages of a conversation stay together
// Distributes load across tenants
sh.shardCollection("emis_chat.messages", { tenantId: 1, conversationId: 1 });
```

---

## 🔍 Search Strategy

### Phase 1: MongoDB Text Search

```csharp
public interface IMessageSearchService
{
    Task<SearchResult<MessageDto>> SearchMessagesAsync(
        Guid conversationId,
        string searchTerm,
        MessageType? filterType,
        DateTime? fromDate,
        DateTime? toDate,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);
}

public class MongoDbMessageSearchService : IMessageSearchService
{
    // Uses MongoDB $text operator with indexes
    // Supports: full-text search, filters, date ranges
    // Limitations: No advanced relevance scoring, no typo tolerance
}
```

### Phase 2: Elasticsearch (Migration Plan)

```csharp
public class ElasticsearchMessageSearchService : IMessageSearchService
{
    // Advanced features:
    // - Better relevance scoring
    // - Fuzzy search (typo tolerance)
    // - Highlighting
    // - Aggregations
    // - Multi-language support
}

// Migration process:
// 1. Index existing messages to Elasticsearch (background job)
// 2. Real-time indexing on new messages
// 3. Feature flag to switch between implementations
// 4. Gradual rollout per tenant
```

---

## 🔄 Integration Events

### Events Published by Chat Service

```csharp
// When new message sent
public class MessageSentIntegrationEvent : IntegrationEvent
{
    public Guid ConversationId { get; set; }
    public Guid MessageId { get; set; }
    public Guid SenderId { get; set; }
    public string Content { get; set; }
    public List<Guid> RecipientUserIds { get; set; }
    public List<Guid> MentionedUserIds { get; set; }
}

// When message pinned
public class MessagePinnedIntegrationEvent : IntegrationEvent
{
    public Guid ConversationId { get; set; }
    public Guid MessageId { get; set; }
    public string Content { get; set; }
    public List<Guid> ParticipantUserIds { get; set; }
}
```

### Events Consumed by Chat Service

```csharp
// From Student Service
public class ClassCreatedIntegrationEvent : IntegrationEvent
{
    public Guid ClassId { get; set; }
    public string ClassName { get; set; }
    public Guid TenantId { get; set; }
    public Guid PrimaryTeacherId { get; set; }
}

// Handler: Create ClassGroup conversation
public class ClassCreatedIntegrationEventHandler : IIntegrationEventHandler<ClassCreatedIntegrationEvent>
{
    // Create ClassGroup with teacher as admin
}

// From Student Service
public class StudentAssignedToClassIntegrationEvent : IntegrationEvent
{
    public Guid StudentId { get; set; }
    public Guid ClassId { get; set; }
    public List<Guid> ParentUserIds { get; set; }
}

// Handler: Add parents to ClassGroup
public class StudentAssignedToClassIntegrationEventHandler : IIntegrationEventHandler<StudentAssignedToClassIntegrationEvent>
{
    // Add all parent userIds to ClassGroup conversation
}

// From Student Service
public class ParentAddedToStudentIntegrationEvent : IntegrationEvent
{
    public Guid StudentId { get; set; }
    public Guid ParentUserId { get; set; }
    public Guid ClassId { get; set; }
}

// Handler: Add parent to StudentGroup (if exists) and ClassGroup
public class ParentAddedToStudentIntegrationEventHandler : IIntegrationEventHandler<ParentAddedToStudentIntegrationEvent>
{
    // 1. Find StudentGroup by studentId (if exists)
    // 2. Add parent to StudentGroup
    // 3. Find ClassGroup by classId
    // 4. Add parent to ClassGroup
}

// From Teacher Service
public class TeacherAssignedToClassIntegrationEvent : IntegrationEvent
{
    public Guid TeacherId { get; set; }
    public Guid TeacherUserId { get; set; }
    public Guid ClassId { get; set; }
}

// Handler: Add teacher to ClassGroup
public class TeacherAssignedToClassIntegrationEventHandler : IIntegrationEventHandler<TeacherAssignedToClassIntegrationEvent>
{
    // Add teacher to ClassGroup as admin
}
```

---

## 🚀 SignalR Hub Design

### ChatHub

```csharp
public class ChatHub : Hub
{
    private readonly IMediator _mediator;
    private readonly IOnlineUsersService _onlineUsersService;
    
    // Connection Management
    public override async Task OnConnectedAsync();
    public override async Task OnDisconnectedAsync(Exception? exception);
    
    // Messaging
    public async Task SendMessage(SendMessageRequest request);
    public async Task EditMessage(Guid messageId, string newContent);
    public async Task DeleteMessage(Guid messageId);
    public async Task AddReaction(Guid messageId, string emojiCode);
    public async Task ForwardMessage(Guid messageId, Guid targetConversationId);
    
    // Conversation Management
    public async Task JoinConversation(Guid conversationId);
    public async Task LeaveConversation(Guid conversationId);
    
    // Real-time Status
    public async Task SendTypingIndicator(Guid conversationId);
    public async Task MarkMessagesAsRead(Guid conversationId, List<Guid> messageIds);
    
    // Pin Messages
    public async Task PinMessage(Guid messageId);
    public async Task UnpinMessage(Guid messageId);
}
```

### Client Methods (Invoked by Server)

```csharp
// Server -> Client
await Clients.Group(conversationId.ToString())
    .SendAsync("ReceiveMessage", messageDto);

await Clients.Group(conversationId.ToString())
    .SendAsync("UserTyping", userId, userName);

await Clients.Group(conversationId.ToString())
    .SendAsync("MessageRead", messageId, userId, readAt);

await Clients.Group(conversationId.ToString())
    .SendAsync("MessageEdited", messageId, newContent, editedAt);

await Clients.Group(conversationId.ToString())
    .SendAsync("MessageDeleted", messageId);

await Clients.Group(conversationId.ToString())
    .SendAsync("ReactionAdded", messageId, reaction);

await Clients.User(userId.ToString())
    .SendAsync("UserOnline", userId);

await Clients.User(userId.ToString())
    .SendAsync("UserOffline", userId, lastSeenAt);
```

### Redis Backplane Configuration

```csharp
services.AddSignalR()
    .AddStackExchangeRedis(options =>
    {
        options.Configuration.ChannelPrefix = "ChatHub";
        options.Configuration.AbortOnConnectFail = false;
    });
```

---

## 🔐 Authorization Rules

### Permission Matrix

| User Role | OneToOne | StudentGroup | ClassGroup | TeacherGroup | AnnouncementChannel |
|-----------|----------|--------------|------------|--------------|---------------------|
| **Parent** | ✅ (own class) | ✅ (own child) | ✅ (own class) | ❌ | 👁️ Read only |
| **Teacher** | ✅ (own class) | ✅ (own class) | ✅ (own class) | ✅ | ✅ Read/Write |
| **Admin** | ✅ All | ✅ All | ✅ All | ✅ All | ✅ Read/Write |

### Authorization Checks

```csharp
public class ConversationAuthorizationService
{
    // Check if user can access conversation
    public async Task<bool> CanAccessConversationAsync(Guid userId, Guid conversationId);
    
    // Check if parent can chat with teacher
    public async Task<bool> CanParentChatWithTeacherAsync(Guid parentUserId, Guid teacherUserId);
    
    // Check if teacher can view class conversation
    public async Task<bool> CanTeacherAccessClassConversationAsync(Guid teacherUserId, Guid classId);
    
    // Check if user can send message in conversation
    public async Task<bool> CanSendMessageAsync(Guid userId, Guid conversationId);
    
    // Check if admin can view all conversations
    public async Task<bool> IsAdminOrPrincipalAsync(Guid userId);
}
```

---

## 📦 File Storage Strategy

### MinIO Structure

```
Bucket: emis-chat-files
├── {tenantId}/
│   ├── {conversationId}/
│   │   ├── {messageId}/
│   │   │   ├── original/
│   │   │   │   └── {filename}
│   │   │   └── thumbnails/
│   │   │       └── {filename}_thumb.jpg
```

### File Processing Pipeline

```
1. Client uploads file → API
2. Validate: file size, type, virus scan
3. Generate thumbnail (for images/videos)
4. Upload to MinIO
5. Store URL in message
6. Broadcast to conversation
```

### Cleanup Job

```csharp
public class CleanupOldFilesJob
{
    // Run daily at 2 AM
    // Find files older than 1 year
    // Delete from MinIO
    // Update message records (keep metadata)
}
```

---

## 📈 Performance Optimization

### 1. Caching Strategy (Redis)

```csharp
// Active conversations (user's conversation list)
Key: "chat:user:{userId}:conversations"
TTL: 1 hour
Value: List<ConversationDto>

// Online users
Key: "chat:online:{userId}"
TTL: 5 minutes (refreshed on heartbeat)
Value: { lastSeen: DateTime, connectionIds: [] }

// Typing indicators
Key: "chat:typing:{conversationId}:{userId}"
TTL: 5 seconds
Value: userName

// Unread counts
Key: "chat:unread:{userId}"
TTL: 1 hour
Value: Dictionary<ConversationId, Count>

// Recent messages (hot data)
Key: "chat:messages:{conversationId}:recent"
TTL: 30 minutes
Value: List<MessageDto> (last 50 messages)
```

### 2. Connection Management

```csharp
public class ConnectionMappingService
{
    // Track multiple connections per user (multiple tabs/devices)
    private ConcurrentDictionary<Guid, HashSet<string>> _userConnections;
    
    public void AddConnection(Guid userId, string connectionId);
    public void RemoveConnection(Guid userId, string connectionId);
    public IEnumerable<string> GetConnections(Guid userId);
    public bool IsUserOnline(Guid userId);
}
```

### 3. Message Pagination Strategy

```csharp
// Load messages in chunks
public async Task<PagedResult<MessageDto>> GetMessagesAsync(
    Guid conversationId,
    DateTime? beforeTimestamp, // Cursor-based pagination
    int pageSize = 50)
{
    // Query: sentAt < beforeTimestamp, order by sentAt DESC, limit 50
    // More efficient than skip/take for large datasets
}
```

### 4. Database Optimization

```csharp
// Compound indexes for common queries
db.messages.createIndex({ 
    conversationId: 1, 
    sentAt: -1,
    isDeleted: 1 
});

// Partial index for pinned messages (smaller index)
db.messages.createIndex(
    { conversationId: 1, sentAt: -1 },
    { partialFilterExpression: { isPinned: true } }
);
```

---

## 🧪 Testing Strategy

### Unit Tests
- Domain model business rules
- Command/Query handlers
- Authorization rules
- Value object equality

### Integration Tests
- Repository operations
- Event publishing/consuming
- SignalR hub methods
- File upload/download

### Performance Tests
- Load testing: 4M concurrent connections
- Message throughput testing
- Database query performance
- Cache hit ratio testing

---

## 📊 Monitoring & Observability

### Metrics to Track
- Active connections count
- Messages per second
- Average message latency
- SignalR reconnection rate
- Cache hit ratio
- Database query performance
- File upload success rate
- Unread message count distribution

### Logging
- Message sent/received events
- Connection/disconnection events
- Authorization failures
- File upload errors
- Integration event processing

---

## 🚀 Deployment Considerations

### Horizontal Scaling
- Multiple API instances behind load balancer
- Redis backplane for SignalR
- Sticky sessions NOT required (stateless)
- MongoDB replica set for high availability

### Resource Requirements (Estimated)
- **API Instances**: 10+ instances for 4M users
- **MongoDB**: Replica set with 3 nodes, 500GB+ storage
- **Redis**: 16GB+ memory for backplane + cache
- **MinIO**: 1TB+ storage (expandable)

### Health Checks
```csharp
builder.Services.AddHealthChecks()
    .AddMongoDb(mongoConnectionString)
    .AddRedis(redisConnectionString)
    .AddSignalRHub("/hubs/chat");
```

---

## 📅 Implementation Phases

### Phase 1: Core Features (4-6 weeks)
- ✅ Domain models + repositories
- ✅ Basic CQRS commands/queries
- ✅ SignalR hub with basic messaging
- ✅ File upload/storage
- ✅ Conversation types (OneToOne, StudentGroup, ClassGroup)
- ✅ MongoDB text search

### Phase 2: Advanced Features (3-4 weeks)
- ✅ Reactions, mentions, pins
- ✅ Edit/delete messages
- ✅ Forward messages
- ✅ Voice messages
- ✅ Typing indicators, online status
- ✅ Read receipts
- ✅ Authorization rules

### Phase 3: Integration & Optimization (2-3 weeks)
- ✅ Integration events
- ✅ Auto-group creation
- ✅ Notification integration
- ✅ Caching layer
- ✅ Background jobs
- ✅ Performance tuning

### Phase 4: Elasticsearch Migration (2-3 weeks)
- ✅ Elasticsearch setup
- ✅ Indexing pipeline
- ✅ Advanced search features
- ✅ Gradual rollout

---

## 🔗 Related Services

### Dependencies
- **Identity Service**: User authentication/authorization
- **Student Service**: Student, Parent, Class data
- **Teacher Service**: Teacher data, class assignments
- **Notification Service**: Push notifications, badges

### Consumers
- **Report Service**: Chat analytics, message statistics
- **Admin Portal**: Conversation monitoring (admin/principal)

---

## 📚 References

### Technology Stack
- **ASP.NET Core 8.0**: Web API framework
- **SignalR**: Real-time communication
- **MongoDB 7.0**: Document database
- **Redis 7.0**: Caching + SignalR backplane
- **MinIO**: S3-compatible file storage
- **MediatR**: CQRS implementation
- **FluentValidation**: Input validation
- **AutoMapper**: Object mapping
- **Hangfire**: Background jobs
- **Elasticsearch 8.x**: Advanced search (Phase 2)

### Design Patterns
- **Domain-Driven Design (DDD)**
- **CQRS (Command Query Responsibility Segregation)**
- **Event Sourcing (audit trail)**
- **Repository Pattern (encapsulated queries)**
- **Unit of Work Pattern**
- **Outbox Pattern (reliable event publishing)**

---

**Document Version**: 1.0  
**Last Updated**: November 6, 2025  
**Author**: AI Assistant  
**Review Status**: Ready for Implementation


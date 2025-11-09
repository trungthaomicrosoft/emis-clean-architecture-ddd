# Chat Worker Service

## Overview
Chat.Worker là một background worker service chuyên xử lý các internal events của Chat Service. Service này tách biệt với Chat.API để đảm bảo:

- **Separation of Concerns**: API tập trung vào xử lý HTTP/SignalR requests
- **Scalability**: Worker có thể scale độc lập với API
- **Reliability**: Event processing không ảnh hưởng đến API performance

## Architecture

```
┌─────────────┐      Publish Events      ┌──────────────┐
│  Chat.API   │ ──────────────────────> │    Kafka     │
└─────────────┘                          └──────────────┘
                                                │
                                                │ Consume Events
                                                ▼
                                         ┌──────────────┐
                                         │ Chat.Worker  │
                                         └──────────────┘
                                                │
                                                ▼
                                         ┌──────────────┐
                                         │   MongoDB    │
                                         └──────────────┘
```

## Responsibilities

### Chat.API
- Xử lý HTTP requests (REST endpoints)
- Xử lý SignalR connections (real-time messaging)
- Publish events đến Kafka
- Subscribe external integration events (từ Student Service, Teacher Service...)

### Chat.Worker
- Subscribe internal chat events từ Kafka
- Xử lý background tasks:
  - `MessageSentEvent`: Update conversation metadata, notifications
  - Future: Message analytics, content moderation, etc.

## Events Handled

### MessageSentEvent
Published khi có tin nhắn mới được gửi:
```csharp
public class MessageSentEvent : IntegrationEvent
{
    public Guid MessageId { get; set; }
    public Guid ConversationId { get; set; }
    public Guid SenderId { get; set; }
    public string Content { get; set; }
    public DateTime SentAt { get; set; }
}
```

Handler thực hiện:
1. Update conversation's last message
2. Update conversation's last activity timestamp
3. Trigger notifications (if needed)
4. Analytics tracking (future)

## Configuration

### appsettings.json
```json
{
  "KafkaSettings": {
    "BootstrapServers": "localhost:9092",
    "GroupId": "emis-chat-worker",
    "ClientId": "chat-worker-consumer",
    "Topics": [
      "emis.message.sent"
    ]
  }
}
```

### Environment Variables (Docker)
- `DOTNET_ENVIRONMENT`: Development/Production
- `MongoDbSettings__ConnectionString`: MongoDB connection
- `KafkaSettings__BootstrapServers`: Kafka brokers
- `RedisSettings__ConnectionString`: Redis connection

## Running Locally

### Prerequisites
- .NET 8.0 SDK
- Kafka running (via docker-compose)
- MongoDB running

### Start Worker
```bash
cd src/Services/Chat/Chat.Worker
dotnet run
```

### With Docker Compose
```bash
docker-compose up chat-worker
```

## Deployment

Worker service runs as a separate container:
- Auto-scales based on Kafka consumer lag
- Independent health monitoring
- Can be deployed to Kubernetes as a Deployment (vs API as a Service)

## Monitoring

### Health Checks
- `/health` endpoint (if needed for orchestration)
- Kafka consumer lag metrics
- Event processing success/failure rates

### Logs
Logs written to:
- Console (stdout)
- File: `Logs/chat-worker-.log` (daily rotation)

Log levels:
- **Info**: Event received, processing started
- **Warning**: Retryable errors
- **Error**: Processing failures
- **Fatal**: Worker crashes

## Future Enhancements

1. **Message Analytics**
   - Track message volumes
   - User engagement metrics
   - Popular conversation patterns

2. **Content Moderation**
   - Scan for inappropriate content
   - Auto-flag suspicious messages
   - AI-based content filtering

3. **Smart Notifications**
   - Batch notifications
   - User preference handling
   - Notification throttling

4. **Data Archiving**
   - Move old messages to cold storage
   - Conversation history cleanup
   - Compliance data retention

## Development Guidelines

### Adding New Event Handlers

1. Create event class in `Chat.Application/Events/`:
```csharp
public class NewChatEvent : IntegrationEvent
{
    // Properties
}
```

2. Create handler in `Chat.Application/Events/Handlers/`:
```csharp
public class NewChatEventHandler : IIntegrationEventHandler<NewChatEvent>
{
    public async Task Handle(NewChatEvent @event)
    {
        // Processing logic
    }
}
```

3. Register in `Chat.Worker/Program.cs`:
```csharp
consumer.Subscribe<NewChatEvent, NewChatEventHandler>();
```

4. Update topics in appsettings.json

### Testing
```bash
# Unit tests
dotnet test

# Integration tests with Kafka
# (Use Testcontainers for Kafka in tests)
```

## Troubleshooting

### Worker not consuming events
1. Check Kafka connection
2. Verify consumer group ID
3. Check topic exists: `kafka-topics.sh --list`
4. Verify events published to correct topic

### High consumer lag
1. Scale worker instances
2. Optimize event handlers
3. Add batch processing
4. Review database performance

### MongoDB connection issues
1. Verify connection string
2. Check network connectivity
3. Ensure MongoDB indexes created
4. Review connection pool settings

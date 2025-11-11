# Quick Example: Kafka Configuration

## 🎯 Scenario: Chat Service (Producer + Consumer)

Chat service cần:
- **Publish**: MessageSentEvent khi user gửi tin nhắn
- **Subscribe**: StudentCreatedIntegrationEvent để tự động tạo group chat cho học sinh mới

---

## ✅ NEW WAY (Recommended)

### 1. appsettings.json
```json
{
  "Kafka": {
    "BootstrapServers": "localhost:9092",
    "TopicPrefix": "emis",
    "ServiceName": "chat",
    "EventTopicMappings": {
      "MessageSentEvent": "emis.chat.messages"
    },
    
    "Producer": {
      "ClientId": "chat-producer"
    },
    
    "Consumer": {
      "GroupId": "emis-chat-service",
      "ClientId": "chat-consumer"
    }
  }
}
```

### 2. Program.cs
```csharp
// Consumer (Subscribe to events from other services)
builder.Services.AddKafkaConsumer(
    builder.Configuration,
    consumer =>
    {
        consumer.Subscribe<StudentCreatedIntegrationEvent, 
            StudentCreatedIntegrationEventHandler>();
    });

// Producer (Publish own events)
builder.Services.AddKafkaEventBus(builder.Configuration);
```

### 3. Publish Event (trong Command Handler)
```csharp
public class SendMessageCommandHandler : IRequestHandler<SendMessageCommand, ApiResponse<MessageDto>>
{
    private readonly IKafkaEventBus _eventBus;
    
    public async Task<ApiResponse<MessageDto>> Handle(...)
    {
        // ... business logic
        
        // Publish event
        var @event = new MessageSentEvent
        {
            MessageId = message.Id,
            ConversationId = message.ConversationId,
            SenderId = message.SenderId,
            Content = message.Content
        };
        
        await _eventBus.PublishAsync(@event);
        
        return ApiResponse<MessageDto>.SuccessResult(dto);
    }
}
```

### 4. Handle Event (Event Handler)
```csharp
public class StudentCreatedIntegrationEventHandler 
    : IIntegrationEventHandler<StudentCreatedIntegrationEvent>
{
    private readonly IConversationRepository _conversationRepository;
    
    public async Task HandleAsync(
        StudentCreatedIntegrationEvent @event, 
        CancellationToken cancellationToken)
    {
        // Auto-create group chat for new student
        var conversation = new Conversation(
            tenantId: @event.TenantId,
            name: $"Class {@event.ClassName}",
            type: ConversationType.Group
        );
        
        await _conversationRepository.AddAsync(conversation);
    }
}
```

---

## ❌ OLD WAY (Still works, but verbose)

### Program.cs
```csharp
// Consumer
builder.Services.AddKafkaConsumer(
    settings =>
    {
        settings.BootstrapServers = builder.Configuration["KafkaSettings:BootstrapServers"] ?? "localhost:9092";
        settings.GroupId = builder.Configuration["KafkaSettings:GroupId"] ?? "emis-chat-service";
        settings.ClientId = builder.Configuration["KafkaSettings:ClientId"] ?? "chat-consumer";
        settings.TopicPrefix = builder.Configuration["KafkaSettings:TopicPrefix"] ?? "emis";
        settings.ServiceName = builder.Configuration["KafkaSettings:ServiceName"];
    },
    consumer =>
    {
        consumer.Subscribe<StudentCreatedIntegrationEvent, 
            StudentCreatedIntegrationEventHandler>();
    });

// Producer
builder.Services.AddKafkaEventBus(settings =>
{
    settings.BootstrapServers = builder.Configuration["KafkaSettings:BootstrapServers"] ?? "localhost:9092";
    settings.ClientId = builder.Configuration["KafkaSettings:ProducerClientId"] ?? "chat-producer";
    settings.TopicPrefix = builder.Configuration["KafkaSettings:TopicPrefix"] ?? "emis";
    settings.ServiceName = builder.Configuration["KafkaSettings:ServiceName"];
    settings.EventTopicMappings = builder.Configuration
        .GetSection("KafkaSettings:EventTopicMappings")
        .Get<Dictionary<string, string>>() ?? new();
});
```

---

## 📊 Comparison

| Aspect | Old Way | New Way |
|--------|---------|---------|
| **Lines of Code (Program.cs)** | 25+ lines | 8 lines |
| **Config Reading** | Manual with `builder.Configuration["..."]` | Automatic via `Bind()` |
| **Type Safety** | Runtime errors if typo | Compile-time validation |
| **DRY Principle** | Duplicate shared settings | Shared settings in one place |
| **Maintenance** | Hard to update (scattered config) | Easy (centralized in appsettings.json) |

---

## 🚀 Benefits of New Way

1. **Less Code**: 70% reduction in boilerplate
2. **Cleaner**: Configuration in appsettings.json, not in code
3. **Type-Safe**: Automatic binding with validation
4. **Flexible**: Easy to override per-environment (appsettings.Development.json)
5. **DRY**: Shared settings defined once

---

## 🎯 Next Steps

1. Copy example appsettings.json to your service
2. Update section name from "KafkaSettings" to "Kafka"
3. Organize into Producer/Consumer subsections
4. Update Program.cs to use `builder.Configuration` overload
5. Test and deploy!

# Kafka EventBus Configuration Guide

## 📋 Table of Contents
- [Quick Start](#quick-start)
- [Configuration Methods](#configuration-methods)
- [Usage Examples](#usage-examples)
- [Migration Guide](#migration-guide)

---

## 🚀 Quick Start

### Service vừa Publish vừa Subscribe

**1. Add to `appsettings.json`:**
```json
{
  "Kafka": {
    "BootstrapServers": "localhost:9092",
    "TopicPrefix": "emis",
    "ServiceName": "chat",
    
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

**2. Register in `Program.cs`:**
```csharp
// Consumer (Subscribe to events)
builder.Services.AddKafkaConsumer(
    builder.Configuration,
    consumer =>
    {
        consumer.Subscribe<StudentCreatedIntegrationEvent, 
            StudentCreatedIntegrationEventHandler>();
        consumer.Subscribe<TeacherCreatedIntegrationEvent,
            TeacherCreatedIntegrationEventHandler>();
    });

// Producer (Publish events)
builder.Services.AddKafkaEventBus(builder.Configuration);
```

**3. Publish events:**
```csharp
public class SendMessageCommandHandler
{
    private readonly IKafkaEventBus _eventBus;
    
    public async Task Handle(...)
    {
        var @event = new MessageSentEvent { ... };
        await _eventBus.PublishAsync(@event);
    }
}
```

**4. Handle events:**
```csharp
public class StudentCreatedIntegrationEventHandler 
    : IIntegrationEventHandler<StudentCreatedIntegrationEvent>
{
    public async Task HandleAsync(StudentCreatedIntegrationEvent @event, ...)
    {
        // Process event
    }
}
```

---

## ⚙️ Configuration Methods

### Method 1: IConfiguration (Recommended) ✅

**Pros:**
- Clean, declarative configuration
- Easy to manage different environments
- Type-safe with validation
- DRY principle (shared settings)

**Program.cs:**
```csharp
// Producer
builder.Services.AddKafkaEventBus(builder.Configuration);

// Consumer
builder.Services.AddKafkaConsumer(
    builder.Configuration,
    consumer => { /* subscriptions */ });

// Custom section name
builder.Services.AddKafkaEventBus(builder.Configuration, "MyKafkaSettings");
```

### Method 2: Action<Settings> (Legacy)

**Pros:**
- Backward compatible
- Programmatic configuration
- Good for testing

**Program.cs:**
```csharp
// Producer
builder.Services.AddKafkaEventBus(settings =>
{
    settings.BootstrapServers = "localhost:9092";
    settings.ClientId = "chat-producer";
    settings.TopicPrefix = "emis";
});

// Consumer
builder.Services.AddKafkaConsumer(
    settings =>
    {
        settings.BootstrapServers = "localhost:9092";
        settings.GroupId = "emis-chat-service";
        settings.ClientId = "chat-consumer";
        settings.TopicPrefix = "emis";
    },
    consumer => { /* subscriptions */ });
```

---

## 📖 Usage Examples

### Example 1: Minimal Configuration

```json
{
  "Kafka": {
    "BootstrapServers": "localhost:9092",
    "ServiceName": "student",
    
    "Producer": {
      "ClientId": "student-producer"
    },
    
    "Consumer": {
      "GroupId": "emis-student-service",
      "ClientId": "student-consumer"
    }
  }
}
```

### Example 2: Advanced Configuration

```json
{
  "Kafka": {
    "BootstrapServers": "kafka1:9092,kafka2:9092",
    "TopicPrefix": "prod.emis",
    "ServiceName": "payment",
    "DefaultTopicStrategy": "event",
    "EventTopicMappings": {
      "PaymentProcessedEvent": "prod.emis.payments.high-priority",
      "PaymentFailedEvent": "prod.emis.payments.alerts"
    },
    
    "Producer": {
      "ClientId": "payment-producer",
      "Acks": "All",
      "EnableIdempotence": true,
      "MessageTimeoutMs": 60000
    },
    
    "Consumer": {
      "GroupId": "emis-payment-service",
      "ClientId": "payment-consumer",
      "AutoOffsetReset": "Latest",
      "EnableAutoCommit": false
    }
  }
}
```

### Example 3: Producer Can Override Shared Settings

```json
{
  "Kafka": {
    "BootstrapServers": "localhost:9092",
    "TopicPrefix": "emis",
    
    "Producer": {
      "ClientId": "critical-producer",
      "BootstrapServers": "kafka-premium:9092",
      "TopicPrefix": "critical",
      "Acks": "All"
    },
    
    "Consumer": {
      "GroupId": "emis-service",
      "ClientId": "normal-consumer"
    }
  }
}
```

---

## 🔄 Migration Guide

### From Old Config to New Config

**Old Way (Action-based):**
```csharp
builder.Services.AddKafkaEventBus(settings =>
{
    settings.BootstrapServers = builder.Configuration["KafkaSettings:BootstrapServers"]!;
    settings.ClientId = builder.Configuration["KafkaSettings:ProducerClientId"]!;
    settings.TopicPrefix = builder.Configuration["KafkaSettings:TopicPrefix"]!;
});
```

**New Way (Configuration-based):**
```csharp
builder.Services.AddKafkaEventBus(builder.Configuration);
```

**Migration Steps:**

1. **Update appsettings.json:**
```json
{
  "Kafka": {
    "BootstrapServers": "localhost:9092",
    "TopicPrefix": "emis",
    "ServiceName": "yourservice",
    
    "Producer": {
      "ClientId": "yourservice-producer"
    }
  }
}
```

2. **Update Program.cs:**
```csharp
// Old
builder.Services.AddKafkaEventBus(settings => { ... });

// New
builder.Services.AddKafkaEventBus(builder.Configuration);
```

3. **Test:** Run your service and verify it connects to Kafka

---

## 📝 Configuration Reference

### Shared Settings (Base)

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `BootstrapServers` | string | `localhost:9092` | Kafka broker addresses |
| `TopicPrefix` | string | `emis` | Topic naming prefix |
| `ServiceName` | string? | null | Service identifier |
| `DefaultTopicStrategy` | string | `service` | Topic routing: `service` or `event` |
| `EventTopicMappings` | Dictionary | `{}` | Override topic for specific events |

### Producer Settings

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ClientId` | string | `emis-producer` | Producer identifier |
| `Acks` | string | `All` | Acknowledgment: `All`, `Leader`, `None` |
| `EnableIdempotence` | bool | `true` | Prevent duplicate messages |
| `MessageTimeoutMs` | int | `30000` | Message delivery timeout |
| `RequestTimeoutMs` | int | `30000` | Broker request timeout |

### Consumer Settings

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `GroupId` | string | `emis-consumer-group` | Consumer group ID |
| `ClientId` | string | `emis-consumer` | Consumer identifier |
| `AutoOffsetReset` | string | `Earliest` | Start position: `Earliest` or `Latest` |
| `EnableAutoCommit` | bool | `false` | Auto commit offsets |
| `EnableAutoOffsetStore` | bool | `false` | Auto store offsets |
| `ConsumerTimeoutMs` | int | `100` | Message poll timeout |

---

## 🎯 Topic Strategies

### Service-based (Default)
Groups events by service. One topic per service.

**Example:** `emis.chat`, `emis.student`, `emis.payment`

**Config:**
```json
{
  "DefaultTopicStrategy": "service",
  "ServiceName": "chat"
}
```

**Result:** All events → `emis.chat` topic

### Event-based
One topic per event type. Fine-grained control.

**Example:** `emis.messagesent`, `emis.studentcreated`

**Config:**
```json
{
  "DefaultTopicStrategy": "event"
}
```

**Result:** 
- `MessageSentEvent` → `emis.messagesent`
- `StudentCreatedEvent` → `emis.studentcreated`

### Custom Mapping (Override)
Override specific events to dedicated topics.

**Config:**
```json
{
  "DefaultTopicStrategy": "service",
  "ServiceName": "chat",
  "EventTopicMappings": {
    "MessageSentEvent": "emis.chat.messages",
    "UserTypingEvent": "emis.chat.realtime"
  }
}
```

**Result:**
- `MessageSentEvent` → `emis.chat.messages` (custom)
- `UserTypingEvent` → `emis.chat.realtime` (custom)
- Other events → `emis.chat` (default)

---

## 🔧 Troubleshooting

### Producer not connecting
```csharp
// Check logs for:
// "Kafka Producer initialized. Servers: localhost:9092, ClientId: chat-producer"

// Verify config:
builder.Services.AddKafkaEventBus(builder.Configuration);
```

### Consumer not receiving messages
```csharp
// Check logs for:
// "Kafka Consumer started. Group: emis-chat-service, Topics: emis.chat"

// Verify subscriptions:
builder.Services.AddKafkaConsumer(
    builder.Configuration,
    consumer =>
    {
        consumer.Subscribe<MyEvent, MyHandler>(); // Don't forget this!
    });
```

### Topics not created
- Kafka auto-creates topics by default
- Check `server.properties`: `auto.create.topics.enable=true`
- Or create manually: `kafka-topics.sh --create --topic emis.chat`

---

## 📚 See Also

- [appsettings.kafka.example.json](./appsettings.kafka.example.json) - Complete config examples
- [Student Service](../../Services/Student/) - Reference implementation
- [Chat Service](../../Services/Chat/) - Producer + Consumer example

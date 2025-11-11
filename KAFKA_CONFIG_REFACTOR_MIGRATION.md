# Kafka EventBus Configuration Refactor - Migration Guide

## 🎯 What Changed?

### Summary
Refactored Kafka configuration to support **clean, hierarchical settings** with shared base config and service-specific overrides. Now supports both **IConfiguration-based** (recommended) and **Action-based** (backward compatible) registration.

### Key Improvements
1. ✅ **DRY Principle**: Shared settings (BootstrapServers, TopicPrefix, etc.) defined once
2. ✅ **Type Safety**: Separate `KafkaProducerSettings` and `KafkaConsumerSettings` classes
3. ✅ **Flexibility**: Producer/Consumer can override shared settings if needed
4. ✅ **Backward Compatible**: Old Action-based API still works
5. ✅ **Configuration First**: Recommended to use appsettings.json (easier to manage)

---

## 📦 Files Changed

### New Files Created:
1. `src/BuildingBlocks/EMIS.EventBus.Kafka/KafkaBaseSettings.cs`
   - Base class with shared settings
   - `KafkaProducerSettings` (inherits from base)
   - `KafkaConsumerSettings` (inherits from base)

### Modified Files:
1. `src/BuildingBlocks/EMIS.EventBus.Kafka/KafkaEventBus.cs`
   - Uses `KafkaProducerSettings` instead of `KafkaSettings`
   - Configurable Acks, MessageTimeout, RequestTimeout

2. `src/BuildingBlocks/EMIS.EventBus.Kafka/KafkaConsumerService.cs`
   - Uses enhanced `KafkaConsumerSettings` from `KafkaBaseSettings.cs`
   - Configurable AutoOffsetReset, EnableAutoCommit

3. `src/BuildingBlocks/EMIS.EventBus.Kafka/KafkaEventBusExtensions.cs`
   - Added new overloads: `AddKafkaEventBus(IConfiguration)`
   - Added new overloads: `AddKafkaConsumer(IConfiguration, Action)`
   - Kept old overloads for backward compatibility

4. `src/BuildingBlocks/EMIS.EventBus.Kafka/EMIS.EventBus.Kafka.csproj`
   - Added `Microsoft.Extensions.Configuration.Binder` package

### Documentation Files:
1. `src/BuildingBlocks/EMIS.EventBus.Kafka/README.md`
2. `src/BuildingBlocks/EMIS.EventBus.Kafka/appsettings.kafka.example.json`

---

## 🔄 Migration Steps

### Option 1: Migrate to New IConfiguration-based API (Recommended)

#### Before (Old Way):
**Program.cs:**
```csharp
builder.Services.AddKafkaEventBus(settings =>
{
    settings.BootstrapServers = builder.Configuration["KafkaSettings:BootstrapServers"] ?? "localhost:9092";
    settings.ClientId = builder.Configuration["KafkaSettings:ClientId"] ?? "student-producer";
    settings.TopicPrefix = builder.Configuration["KafkaSettings:TopicPrefix"] ?? "emis";
});
```

**appsettings.json:**
```json
{
  "KafkaSettings": {
    "BootstrapServers": "localhost:9092",
    "ClientId": "student-producer",
    "TopicPrefix": "emis"
  }
}
```

#### After (New Way):
**Program.cs:**
```csharp
builder.Services.AddKafkaEventBus(builder.Configuration);
```

**appsettings.json:**
```json
{
  "Kafka": {
    "BootstrapServers": "localhost:9092",
    "TopicPrefix": "emis",
    "ServiceName": "student",
    
    "Producer": {
      "ClientId": "student-producer"
    }
  }
}
```

### Option 2: Keep Using Action-based API (No Changes Needed)

Your existing code **continues to work** without any changes:

```csharp
builder.Services.AddKafkaEventBus(settings =>
{
    settings.BootstrapServers = "localhost:9092";
    settings.ClientId = "student-producer";
    settings.TopicPrefix = "emis";
});
```

---

## 📋 Service-by-Service Migration Checklist

### ✅ Student Service (Producer Only)

**Current Status:** Uses Action-based API
**Migration:** Optional (backward compatible)

**If migrating:**

1. Update `Student.API/appsettings.json`:
```json
{
  "Kafka": {
    "BootstrapServers": "localhost:9092",
    "TopicPrefix": "emis",
    "ServiceName": "student",
    
    "Producer": {
      "ClientId": "student-producer"
    }
  }
}
```

2. Update `Student.API/Program.cs`:
```csharp
// Old
builder.Services.AddKafkaEventBus(settings =>
{
    settings.BootstrapServers = builder.Configuration["KafkaSettings:BootstrapServers"] ?? "localhost:9092";
    settings.ClientId = builder.Configuration["KafkaSettings:ClientId"] ?? "student-producer";
    settings.TopicPrefix = builder.Configuration["KafkaSettings:TopicPrefix"] ?? "emis";
});

// New
builder.Services.AddKafkaEventBus(builder.Configuration);
```

---

### ✅ Chat.Worker (Consumer Only)

**Current Status:** Uses Action-based API
**Migration:** Optional (backward compatible)

**If migrating:**

1. Update `Chat.Worker/appsettings.json`:
```json
{
  "Kafka": {
    "BootstrapServers": "localhost:9092",
    "TopicPrefix": "emis",
    "ServiceName": "chat",
    
    "Consumer": {
      "GroupId": "emis-chat-worker",
      "ClientId": "chat-worker-consumer"
    }
  }
}
```

2. Update `Chat.Worker/Program.cs`:
```csharp
// Old
builder.Services.AddKafkaConsumer(
    settings =>
    {
        settings.BootstrapServers = builder.Configuration["KafkaSettings:BootstrapServers"] ?? "localhost:9092";
        settings.GroupId = builder.Configuration["KafkaSettings:GroupId"] ?? "emis-chat-worker";
        settings.ClientId = builder.Configuration["KafkaSettings:ClientId"] ?? "chat-worker-consumer";
        settings.Topics = builder.Configuration.GetSection("KafkaSettings:Topics").Get<List<string>>() 
            ?? new List<string> { "emis.message.sent" };
    },
    consumer =>
    {
        consumer.Subscribe<MessageSentEvent, MessageSentEventHandler>();
    });

// New
builder.Services.AddKafkaConsumer(
    builder.Configuration,
    consumer =>
    {
        consumer.Subscribe<MessageSentEvent, MessageSentEventHandler>();
    });
```

---

### ✅ Services with Both Producer + Consumer

**Example: Chat.API (if adding both)**

**appsettings.json:**
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

**Program.cs:**
```csharp
// Consumer
builder.Services.AddKafkaConsumer(
    builder.Configuration,
    consumer =>
    {
        consumer.Subscribe<StudentCreatedIntegrationEvent, 
            StudentCreatedIntegrationEventHandler>();
    });

// Producer
builder.Services.AddKafkaEventBus(builder.Configuration);
```

---

## 🧪 Testing Your Migration

### 1. Build Project
```bash
dotnet build
```

### 2. Check Logs
When service starts, you should see:
```
[INFO] Kafka Producer initialized. Servers: localhost:9092, ClientId: chat-producer, Acks: All, Idempotence: True
[INFO] Kafka Consumer started. Group: emis-chat-service, Topics: emis.chat, Strategy: auto-resolved
```

### 3. Verify Topic Creation
```bash
docker exec -it kafka kafka-topics --bootstrap-server localhost:9092 --list
```

Should show topics like: `emis.chat`, `emis.student`, etc.

### 4. Test Event Publishing
Send a command that publishes an event, check logs:
```
[INFO] Published event StudentCreatedIntegrationEvent to Kafka topic emis.student. Partition: 0, Offset: 42
```

### 5. Test Event Consumption
Publish an event from another service, check consumer logs:
```
[INFO] Processing message from topic emis.student, partition 0, offset 42. Event: StudentCreatedIntegrationEvent
[INFO] Successfully processed event StudentCreatedIntegrationEvent
```

---

## ⚠️ Breaking Changes

### None! 🎉

All changes are **backward compatible**. Your existing code will continue to work without modifications.

The new IConfiguration-based API is **opt-in** and recommended for new code.

---

## 📝 Configuration Reference

See [README.md](./README.md) for complete configuration reference.

### Quick Examples:

**Minimal:**
```json
{
  "Kafka": {
    "BootstrapServers": "localhost:9092",
    "ServiceName": "myservice",
    "Producer": { "ClientId": "myservice-producer" }
  }
}
```

**With Overrides:**
```json
{
  "Kafka": {
    "BootstrapServers": "localhost:9092",
    "TopicPrefix": "emis",
    
    "Producer": {
      "ClientId": "critical-producer",
      "BootstrapServers": "kafka-premium:9092",
      "Acks": "All"
    }
  }
}
```

---

## 🐛 Troubleshooting

### Build Errors After Update

**Error:** `Cannot resolve type 'KafkaSettings'`
**Fix:** You're using the old `Action<KafkaSettings>` API. Change to `Action<KafkaProducerSettings>` or use new IConfiguration API.

**Error:** `'Get' does not contain a definition`
**Fix:** Run `dotnet restore` to get `Microsoft.Extensions.Configuration.Binder` package.

### Runtime Errors

**Error:** `No configuration found for 'Kafka' section`
**Fix:** Add `Kafka` section to appsettings.json or use Action-based API.

**Error:** `ClientId is required`
**Fix:** Add `ClientId` to Producer or Consumer section in appsettings.json.

---

## 📞 Support

For questions or issues:
1. Check [README.md](./README.md) for examples
2. Check [appsettings.kafka.example.json](./appsettings.kafka.example.json) for config samples
3. Review Student Service for reference implementation

---

## 🎯 Recommendations

1. ✅ **Use IConfiguration-based API** for new code (cleaner, easier to maintain)
2. ✅ **Migrate gradually** - no rush, backward compatible
3. ✅ **Test in dev environment** before migrating production configs
4. ✅ **Use shared settings** to avoid duplication
5. ✅ **Override only when needed** (Producer/Consumer specific settings)

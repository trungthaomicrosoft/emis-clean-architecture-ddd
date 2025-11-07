# Event-Driven Auto Student Group Creation

## 🎯 Tổng Quan

Đã implement **Event-Driven Architecture** để tự động tạo Student Group Conversation khi có học sinh mới được tạo trong hệ thống.

## 📊 Luồng Hoạt Động

```
┌─────────────────┐         ┌──────────────┐         ┌──────────────┐
│ Student Service │ ─────>  │    Kafka     │ ─────>  │ Chat Service │
│ (Producer)      │ Event   │   Topic      │  Event  │ (Consumer)   │
└─────────────────┘         └──────────────┘         └──────────────┘
        │                                                     │
        │ 1. Create Student                                  │
        │    + Parents                                       │
        │                                                    │
        │ 2. Publish Event                                   │
        │    StudentCreatedIntegrationEvent                  │
        │    └─ StudentId                                    │
        │    └─ StudentName                                  │
        │    └─ ClassId                                      │
        │    └─ Parents[]                                    │
        │                                                    │
        │                                                    │ 3. Consume Event
        │                                                    │
        │                                                    │ 4. Fetch Teachers
        │                                                    │    from Teacher Service
        │                                                    │
        │                                                    │ 5. Create Student Group
        │                                                    │    Auto-generated
        │                                                    └─ Conversation Created!
```

## 🏗️ Architecture Components

### 1. Integration Event (EMIS.EventBus)

**File**: `src/BuildingBlocks/EMIS.EventBus/IntegrationEvents/StudentCreatedIntegrationEvent.cs`

```csharp
public class StudentCreatedIntegrationEvent : IntegrationEvent
{
    public Guid StudentId { get; set; }
    public Guid TenantId { get; set; }
    public Guid ClassId { get; set; }
    public string StudentName { get; set; }
    public List<ParentInfo> Parents { get; set; }
    public Guid CreatedBy { get; set; }
}
```

**Kafka Topic**: `emis.student.created`

---

### 2. Student Service (Event Publisher)

#### Files Modified:
- `Student.Application/Handlers/Students/CreateStudentCommandHandler.cs`
- `Student.Application/Student.Application.csproj` (added EMIS.EventBus reference)
- `Student.API/Program.cs` (registered Kafka producer)
- `Student.API/Student.API.csproj` (added EMIS.EventBus reference)
- `Student.API/appsettings.json` (added Kafka config)

#### Logic:
```csharp
// After saving student to database
var integrationEvent = new StudentCreatedIntegrationEvent(
    student.Id,
    tenantId,
    classId,
    studentName,
    parents.Select(p => new ParentInfo(...)).ToList(),
    createdBy
);

await _eventBus.PublishAsync(integrationEvent, cancellationToken);
```

#### Configuration:
```json
{
  "KafkaSettings": {
    "BootstrapServers": "localhost:9092",
    "ClientId": "student-producer",
    "TopicPrefix": "emis"
  }
}
```

---

### 3. Chat Service (Event Consumer)

#### New Files Created:

**A. Integration Event Handler**
- `Chat.Application/IntegrationEvents/Handlers/StudentCreatedIntegrationEventHandler.cs`

```csharp
public class StudentCreatedIntegrationEventHandler 
    : IIntegrationEventHandler<StudentCreatedIntegrationEvent>
{
    public async Task Handle(StudentCreatedIntegrationEvent @event)
    {
        // 1. Validate parents exist
        // 2. Send CreateStudentGroupFromEventCommand
        // 3. Log result
    }
}
```

**B. Dedicated Command for Event-Driven Flow**
- `Chat.Application/Commands/Conversations/CreateStudentGroupFromEventCommand.cs`
- `Chat.Application/Commands/Conversations/CreateStudentGroupFromEventCommandHandler.cs`

**Difference from Manual Command**:
- ✅ Parent info already in event → No need to call Student Service
- ✅ Only fetch Teachers from Teacher Service
- ✅ More resilient to Student Service downtime

#### Files Modified:
- `Chat.Application/DependencyInjection.cs` (registered event handlers)
- `Chat.Application/Chat.Application.csproj` (added EMIS.EventBus reference)
- `Chat.API/Program.cs` (registered Kafka consumer)
- `Chat.API/appsettings.json` (added Kafka config)

#### Kafka Consumer Registration:
```csharp
builder.Services.AddKafkaConsumer(
    settings =>
    {
        settings.BootstrapServers = "localhost:9092";
        settings.GroupId = "emis-chat-service";
        settings.Topics = new List<string> { "emis.student.created" };
    },
    consumer =>
    {
        consumer.Subscribe<StudentCreatedIntegrationEvent, 
            StudentCreatedIntegrationEventHandler>();
    });
```

#### Configuration:
```json
{
  "KafkaSettings": {
    "BootstrapServers": "localhost:9092",
    "GroupId": "emis-chat-service",
    "ClientId": "chat-consumer",
    "Topics": ["emis.student.created"]
  }
}
```

---

## 🔄 Two Ways to Create Student Group

### 1. **Event-Driven (Automatic)** ⭐ NEW
```
Admin creates Student → Student Service → Kafka Event → Chat Service → Auto-create Group
```

**Characteristics**:
- ✅ Fully automated
- ✅ Asynchronous
- ✅ Eventual consistency
- ✅ Parent info from event (no extra service call)
- ✅ Resilient to Student Service downtime

**Use Case**: 
- Khi admin/teacher tạo học sinh mới trong hệ thống
- Tự động tạo group để phụ huynh và giáo viên giao tiếp

### 2. **HTTP API (Manual)** (Existing)
```
User clicks "Create Group" → Chat API → Fetch Student + Parents + Teachers → Create Group
```

**Characteristics**:
- ⚡ Synchronous response
- 🔍 Full validation immediately
- 📞 Requires Student Service + Teacher Service online

**Use Case**:
- Khi user chủ động tạo conversation từ app
- Khi cần tạo lại group bị xóa

---

## 📦 Package Dependencies Added

### Chat.Application
```xml
<ProjectReference Include="..\..\..\BuildingBlocks\EMIS.EventBus\EMIS.EventBus.csproj" />
<PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="9.0.0" />
```

### Student.Application
```xml
<ProjectReference Include="..\..\..\BuildingBlocks\EMIS.EventBus\EMIS.EventBus.csproj" />
```

### Student.API
```xml
<ProjectReference Include="..\..\..\BuildingBlocks\EMIS.EventBus\EMIS.EventBus.csproj" />
```

---

## 🚀 Deployment Requirements

### Infrastructure
```bash
# Start Kafka (via docker-compose)
docker-compose up -d kafka

# Verify Kafka is running
docker ps | grep kafka
```

### Service Startup Order
```
1. Kafka (port 9092)
2. Student Service (port 5002) - Producer
3. Chat Service (port 5004) - Consumer
```

---

## 🧪 Testing

### Test Scenario 1: Create Student via API

**Request**:
```bash
POST http://localhost:5002/api/students
{
  "fullName": "Nguyễn Văn A",
  "classId": "...",
  "parents": [
    {
      "fullName": "Nguyễn Văn X",
      "phoneNumber": "0123456789",
      "relation": 1
    }
  ]
}
```

**Expected Flow**:
1. ✅ Student created in Student Service
2. ✅ Event published to Kafka topic `emis.student.created`
3. ✅ Chat Service consumes event
4. ✅ Student group auto-created with:
   - Student: Nguyễn Văn A
   - Parent: Nguyễn Văn X
   - Teachers: Fetched from Teacher Service by ClassId

**Verify**:
```bash
# Check Chat Service logs
tail -f logs/chat-service-*.log | grep "StudentCreatedIntegrationEvent"

# Expected output:
# [INFO] Received StudentCreatedIntegrationEvent for student {id}
# [INFO] Successfully created student group conversation {id}
```

### Test Scenario 2: Service Resilience

**Test**: Student Service down when event is consumed

**Expected Behavior**:
- ✅ Chat Service still creates group (parent info from event)
- ✅ Only Teacher fetching might fail
- ✅ Group created with parents only
- ⚠️ Log warning: "No teachers found for class"

---

## 📊 Monitoring & Observability

### Kafka Topic Monitoring
```bash
# List topics
kafka-topics --bootstrap-server localhost:9092 --list

# View messages in topic
kafka-console-consumer --bootstrap-server localhost:9092 \
  --topic emis.student.created --from-beginning
```

### Service Logs

**Student Service** (Producer):
```
[INFO] Published StudentCreatedIntegrationEvent for student {id}
```

**Chat Service** (Consumer):
```
[INFO] Subscribed to event StudentCreatedIntegrationEvent
[INFO] Received StudentCreatedIntegrationEvent for student {id}
[INFO] Fetching teachers for class {classId}
[INFO] Successfully created student group conversation {id}
```

---

## 🎯 Benefits

### Before (HTTP Only)
```
Client → Chat Service → Student Service → Teacher Service
         └─ Synchronous
         └─ Tight coupling
         └─ Cascade failures
```

### After (Event-Driven + HTTP)
```
Event-Driven:
Student Service → Kafka → Chat Service
                  └─ Asynchronous
                  └─ Loose coupling
                  └─ Resilient

HTTP (Still available for manual actions):
Client → Chat Service → Student/Teacher Services
```

### Key Improvements
1. ✅ **Loose Coupling**: Services don't depend on each other's uptime
2. ✅ **Scalability**: Can add more consumers without touching producer
3. ✅ **Resilience**: Events stored in Kafka, processed when service recovers
4. ✅ **Auditability**: Event log in Kafka acts as audit trail
5. ✅ **Performance**: Student Service doesn't wait for Chat Service

---

## 🔮 Future Enhancements

### 1. Additional Events
```csharp
// When parent is added to existing student
ParentEnrolledIntegrationEvent 
→ Add parent to existing student group

// When teacher assigned to class
TeacherAssignedToClassEvent 
→ Add teacher to all student groups in that class

// When student changes class
StudentClassChangedEvent 
→ Update group with new teachers
```

### 2. Dead Letter Queue
```csharp
// For failed event processing
if (failedToProcess)
{
    await _eventBus.PublishAsync(
        new DeadLetterEvent(originalEvent, errorMessage));
}
```

### 3. Event Replay
```csharp
// Replay events for data recovery
var fromDate = DateTime.UtcNow.AddDays(-7);
await _eventBus.ReplayEvents<StudentCreatedIntegrationEvent>(fromDate);
```

---

## 📝 Summary

**Files Created**: 6
- `EMIS.EventBus/IntegrationEvents/StudentCreatedIntegrationEvent.cs`
- `Chat.Application/IntegrationEvents/Handlers/StudentCreatedIntegrationEventHandler.cs`
- `Chat.Application/Commands/Conversations/CreateStudentGroupFromEventCommand.cs`
- `Chat.Application/Commands/Conversations/CreateStudentGroupFromEventCommandHandler.cs`
- This documentation file

**Files Modified**: 10
- Chat.Application/DependencyInjection.cs
- Chat.Application/Chat.Application.csproj
- Chat.API/Program.cs
- Chat.API/appsettings.json
- Chat.API/appsettings.Development.json
- Student.Application/Handlers/Students/CreateStudentCommandHandler.cs
- Student.Application/Student.Application.csproj
- Student.API/Program.cs
- Student.API/Student.API.csproj
- Student.API/appsettings.json

**Architecture Pattern**: Event-Driven Microservices ⭐
**Message Broker**: Apache Kafka
**Topic**: `emis.student.created`
**Producer**: Student Service
**Consumer**: Chat Service

---

## ✅ Checklist

- [x] Integration event defined
- [x] Student Service publishes event
- [x] Chat Service subscribes to event
- [x] Event handler implemented
- [x] Dedicated command for event-driven flow
- [x] Kafka configuration added
- [x] Logging implemented
- [x] Error handling for failed events
- [x] Documentation completed

**Status**: ✅ **COMPLETED - Ready for Testing**

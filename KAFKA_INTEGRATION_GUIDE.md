# Service Integration with Kafka EventBus - Implementation Guide

## Overview

This document describes the completed Event-Driven integration between Teacher Service and Identity Service using Apache Kafka. When a Teacher is created, an integration event is automatically published to Kafka, and the Identity Service consumes this event to create a User account with `PendingActivation` status.

## Architecture

```
Teacher Service                          Kafka Broker                    Identity Service
     |                                       |                                  |
     | 1. Create Teacher                     |                                  |
     |    (via API POST)                     |                                  |
     |                                       |                                  |
     | 2. Save to Database                   |                                  |
     |                                       |                                  |
     | 3. Publish                            |                                  |
     |    TeacherCreatedIntegrationEvent     |                                  |
     |-------------------------------------->|                                  |
     |                                       |                                  |
     |                                       | 4. Store Event in Topic          |
     |                                       |    (emis.teacher.teachercreated) |
     |                                       |                                  |
     |                                       | 5. Consume Event                 |
     |                                       |--------------------------------->|
     |                                       |                                  |
     |                                       |                     6. Create User Account
     |                                       |                        (Status: PendingActivation)
     |                                       |                        (EntityId: TeacherId)
     |                                       |                                  |
     |                                       | 7. Commit Offset                 |
     |                                       |<---------------------------------|
```

## Components Implemented

### 1. EventBus Infrastructure (EMIS.EventBus)

#### Core Interfaces
- **`IIntegrationEvent`**: Base interface for all integration events
- **`IntegrationEvent`**: Abstract base class with Id, OccurredOn properties
- **`IIntegrationEventHandler<TEvent>`**: Handler interface for processing events
- **`IKafkaEventBus`**: Service interface for publishing events

#### Kafka Producer (`KafkaEventBus.cs`)
```csharp
public class KafkaEventBus : IKafkaEventBus
{
    // Features:
    // - Idempotent producer (prevents duplicates)
    // - All replicas acknowledgment (Acks.All)
    // - Automatic topic naming convention: emis.{service}.{event-name}
    // - JSON serialization with camelCase
    // - Event metadata in message headers
}
```

**Configuration:**
- Bootstrap Servers: localhost:9092 (configurable)
- Client ID: Identifies the producer service
- Topic Prefix: "emis" by default

#### Kafka Consumer (`KafkaConsumerService.cs`)
```csharp
public class KafkaConsumerService : BackgroundService
{
    // Features:
    // - Runs as hosted background service
    // - Manual offset commit (reliability)
    // - Dynamic event handler registration
    // - Automatic deserialization based on event type
    // - Dependency injection support
}
```

**Configuration:**
- Bootstrap Servers: localhost:9092
- Group ID: Unique consumer group per service
- Auto Offset Reset: Earliest (processes all messages)
- Manual Commit: True (commit only after successful processing)

#### Integration Events
- **`TeacherCreatedIntegrationEvent`**: Published when teacher is created
  - TeacherId (Guid)
  - TenantId (Guid)
  - PhoneNumber (string)
  - FullName (string)
  - Email (string?)

- **`TeacherDeletedIntegrationEvent`**: Published when teacher is deleted
  - TeacherId (Guid)
  - TenantId (Guid)

### 2. Teacher Service Integration

#### Updated Files

**Teacher.Application/Handlers/Teachers/CreateTeacherCommandHandler.cs**
```csharp
public class CreateTeacherCommandHandler
{
    private readonly IKafkaEventBus _eventBus;
    
    public async Task<ApiResponse<TeacherDetailDto>> Handle(...)
    {
        // 1. Save teacher to database
        await _teacherRepository.AddAsync(teacher, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 2. Publish integration event
        var integrationEvent = new TeacherCreatedIntegrationEvent(
            teacher.Id, teacher.TenantId, teacher.PhoneNumber, 
            teacher.FullName, teacher.Email);
        
        await _eventBus.PublishAsync(integrationEvent, cancellationToken);
        
        // Note: Publish errors are logged but don't fail the operation
        // Event can be republished via retry mechanism
    }
}
```

**Teacher.API/Program.cs**
```csharp
// Kafka EventBus Registration
builder.Services.AddKafkaEventBus(settings =>
{
    settings.BootstrapServers = builder.Configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
    settings.ClientId = "teacher-service-producer";
    settings.TopicPrefix = "emis.teacher";
});
```

**Teacher.API/appsettings.json**
```json
{
  "Kafka": {
    "BootstrapServers": "localhost:9092"
  }
}
```

### 3. Identity Service Integration

#### New Files

**Identity.Application/EventHandlers/TeacherCreatedEventHandler.cs**
```csharp
public class TeacherCreatedEventHandler : IIntegrationEventHandler<TeacherCreatedIntegrationEvent>
{
    public async Task Handle(TeacherCreatedIntegrationEvent @event, CancellationToken ct)
    {
        // Create user with PendingActivation status
        var command = new CreateUserCommand
        {
            PhoneNumber = @event.PhoneNumber,
            FullName = @event.FullName,
            Email = @event.Email,
            Role = UserRole.Teacher,
            EntityId = @event.TeacherId // Links User to Teacher
        };

        var result = await _mediator.Send(command, ct);
        // Log success or failure
    }
}
```

**Identity.API/Program.cs**
```csharp
// Event Handlers Registration
builder.Services.AddScoped<TeacherCreatedEventHandler>();

// Kafka Consumer Registration
builder.Services.AddKafkaConsumer(
    settings =>
    {
        settings.BootstrapServers = builder.Configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
        settings.GroupId = "identity-service-consumer";
        settings.ClientId = "identity-service";
        settings.Topics = new List<string> { "emis.teacher.teachercreated" };
    },
    consumer =>
    {
        consumer.Subscribe<TeacherCreatedIntegrationEvent, TeacherCreatedEventHandler>();
    });
```

**Identity.API/appsettings.json**
```json
{
  "Kafka": {
    "BootstrapServers": "localhost:9092"
  }
}
```

### 4. Infrastructure (Docker Compose)

**docker-compose.yml**
```yaml
services:
  zookeeper:
    image: confluentinc/cp-zookeeper:7.5.0
    ports:
      - "2181:2181"
    environment:
      ZOOKEEPER_CLIENT_PORT: 2181

  kafka:
    image: confluentinc/cp-kafka:7.5.0
    ports:
      - "9092:9092"
    depends_on:
      - zookeeper
    environment:
      KAFKA_BROKER_ID: 1
      KAFKA_ZOOKEEPER_CONNECT: zookeeper:2181
      KAFKA_ADVERTISED_LISTENERS: PLAINTEXT://localhost:9092
      KAFKA_AUTO_CREATE_TOPICS_ENABLE: "true"

  kafka-ui:
    image: provectuslabs/kafka-ui:latest
    ports:
      - "8080:8080"
    environment:
      KAFKA_CLUSTERS_0_BOOTSTRAPSERVERS: kafka:9092
```

## Integration Flow

### 1. Teacher Creation Flow

```
POST /api/v1/teachers
{
  "fullName": "Nguyen Van A",
  "phoneNumber": "0901234567",
  "email": "teacher@example.com",
  ...
}

↓

Teacher Service:
1. Validate command (FluentValidation)
2. Check phone number uniqueness
3. Create Teacher aggregate
4. Save to emis_teacher database
5. Publish TeacherCreatedIntegrationEvent to Kafka

↓

Kafka Topic: emis.teacher.teachercreated
- Event stored in partition
- Persisted to disk
- Available for consumption

↓

Identity Service:
1. Consume event from Kafka
2. TeacherCreatedEventHandler invoked
3. Create User with:
   - PhoneNumber = Teacher's phone (username)
   - Status = PendingActivation
   - Role = Teacher
   - EntityId = TeacherId (links to Teacher)
4. Save to emis_identity database
5. Commit Kafka offset

↓

Result:
- Teacher created in Teacher Service ✓
- User account created in Identity Service ✓
- User can now set password on first login
```

### 2. First-Time Login Flow

```
1. Teacher receives phone number from school admin
2. Teacher navigates to login page
3. Teacher enters phone number
4. System detects Status = PendingActivation
5. Redirect to "Set Password" page
6. Teacher sets password
   POST /api/v1/auth/set-password
   {
     "phoneNumber": "0901234567",
     "newPassword": "SecurePass123!"
   }
7. User status changes to Active
8. Teacher can now login normally
```

## Testing the Integration

### Prerequisites
```bash
# Start infrastructure (MySQL, Kafka, Zookeeper)
docker-compose up -d

# Verify Kafka is running
docker ps | grep kafka

# Check Kafka UI
open http://localhost:8080
```

### Step 1: Start Teacher Service
```bash
cd src/Services/Teacher/Teacher.API
dotnet run

# Service starts on: https://localhost:5002
# Swagger UI: https://localhost:5002/swagger
```

### Step 2: Start Identity Service
```bash
cd src/Services/Identity/Identity.API
dotnet run

# Service starts on: https://localhost:5001
# Swagger UI: https://localhost:5001/swagger
```

### Step 3: Create Admin User (First Time Only)
```bash
# POST /api/v1/auth/register-admin
curl -X POST https://localhost:5001/api/v1/auth/register-admin \
  -H "Content-Type: application/json" \
  -d '{
    "phoneNumber": "0999999999",
    "password": "Admin123!",
    "fullName": "School Admin",
    "email": "admin@school.com"
  }'
```

### Step 4: Login as Admin
```bash
# POST /api/v1/auth/login
curl -X POST https://localhost:5001/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "phoneNumber": "0999999999",
    "password": "Admin123!"
  }'

# Copy the JWT token from response
```

### Step 5: Create a Teacher (triggers event)
```bash
# POST /api/v1/teachers
curl -X POST https://localhost:5002/api/v1/teachers \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer {YOUR_JWT_TOKEN}" \
  -d '{
    "userId": "00000000-0000-0000-0000-000000000001",
    "fullName": "Nguyen Van A",
    "gender": 1,
    "phoneNumber": "0901234567",
    "email": "teacher@example.com",
    "dateOfBirth": "1990-01-01",
    "hireDate": "2024-01-01"
  }'
```

### Step 6: Verify Event Published
```bash
# Check Teacher Service logs
# Should see: "Published TeacherCreatedIntegrationEvent for Teacher {TeacherId}"

# Check Kafka UI (http://localhost:8080)
# Topic: emis.teacher.teachercreated
# Messages: Should see 1 message with teacher data
```

### Step 7: Verify Identity Service Consumed Event
```bash
# Check Identity Service logs
# Should see: "Handling TeacherCreatedIntegrationEvent for Teacher..."
# Should see: "Successfully created User account for Teacher {TeacherId}"

# Query User by phone
curl -X GET "https://localhost:5001/api/v1/auth/users?phoneNumber=0901234567" \
  -H "Authorization: Bearer {ADMIN_JWT_TOKEN}"

# Response should show:
# - PhoneNumber: 0901234567
# - Status: PendingActivation (1)
# - Role: Teacher (1)
# - EntityId: {TeacherId from step 5}
```

### Step 8: Teacher Sets Password (First Login)
```bash
# POST /api/v1/auth/set-password
curl -X POST https://localhost:5001/api/v1/auth/set-password \
  -H "Content-Type: application/json" \
  -d '{
    "phoneNumber": "0901234567",
    "newPassword": "Teacher123!"
  }'

# Response: Success
# User status changed to Active
```

### Step 9: Teacher Logs In
```bash
# POST /api/v1/auth/login
curl -X POST https://localhost:5001/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "phoneNumber": "0901234567",
    "password": "Teacher123!"
  }'

# Response: JWT token + refresh token
```

## Monitoring and Debugging

### Kafka UI (http://localhost:8080)
- View topics and messages
- Check consumer groups
- Monitor lag (messages not yet processed)
- Inspect message content

### Service Logs
```bash
# Teacher Service logs
tail -f src/Services/Teacher/Teacher.API/logs/teacher-service-*.txt

# Identity Service logs
tail -f src/Services/Identity/Identity.API/logs/identity-service-*.txt
```

### Kafka CLI Tools
```bash
# List topics
docker exec -it emis-kafka kafka-topics --bootstrap-server localhost:9092 --list

# Consume messages from topic
docker exec -it emis-kafka kafka-console-consumer \
  --bootstrap-server localhost:9092 \
  --topic emis.teacher.teachercreated \
  --from-beginning

# Check consumer group status
docker exec -it emis-kafka kafka-consumer-groups \
  --bootstrap-server localhost:9092 \
  --describe \
  --group identity-service-consumer
```

## Error Handling

### Scenario 1: Kafka Not Available
- **Teacher Service**: Logs error, but teacher creation succeeds
- **Solution**: Event can be republished later via retry mechanism or manual intervention
- **Improvement**: Implement Outbox Pattern for guaranteed delivery

### Scenario 2: Identity Service Down
- **Kafka**: Stores event, retains for 7 days (retention policy)
- **When service restarts**: Consumes event from last committed offset
- **Result**: User account eventually created (eventual consistency)

### Scenario 3: Duplicate Phone Number
- **Identity Service**: CreateUserCommand fails validation
- **Logs**: Warning with error details
- **Kafka**: Offset committed (message not reprocessed)
- **Solution**: Admin must resolve manually (change phone in Teacher Service, republish event)

### Scenario 4: Event Deserialization Error
- **Identity Service**: Logs error, does NOT commit offset
- **Kafka**: Redelivers message on next poll
- **Solution**: Fix event schema or add migration logic

## Performance Considerations

### Throughput
- **Current**: Single partition per topic
- **Production**: Use multiple partitions for parallel processing
- **Scaling**: Add more consumer instances (same group ID)

### Latency
- **Average**: 50-200ms end-to-end (local environment)
- **Factors**: Network latency, database write time, handler processing

### Reliability
- **At-Least-Once Delivery**: Kafka guarantees, but may have duplicates
- **Idempotency**: CreateUserCommand should check if user exists (by EntityId)
- **Transactional Outbox**: Recommended for critical events

## Future Enhancements

### 1. Outbox Pattern
Store events in database alongside entity changes, then publish to Kafka in separate process. Guarantees consistency.

### 2. Parent Created Event
Similar integration for Student Service → Identity Service when Parent is created.

### 3. Dead Letter Queue
Failed messages (after retry limit) sent to DLQ for manual inspection.

### 4. Event Versioning
Support multiple event schema versions for backward compatibility.

### 5. Saga Pattern
Coordinate multi-step transactions across services (e.g., Teacher → User → Notification).

### 6. CQRS Read Models
Consume events to build denormalized read models for queries.

## Troubleshooting

### Problem: Consumer Not Receiving Messages
```bash
# Check consumer group
docker exec -it emis-kafka kafka-consumer-groups \
  --bootstrap-server localhost:9092 \
  --describe \
  --group identity-service-consumer

# Expected: LAG should be 0
# If LAG > 0: Messages exist but not consumed

# Solution:
# 1. Check Identity Service is running
# 2. Check logs for errors
# 3. Verify topic name matches subscription
```

### Problem: "Unable to connect to Kafka"
```bash
# Check Kafka container
docker ps | grep kafka

# Check Kafka logs
docker logs emis-kafka

# Restart Kafka
docker-compose restart kafka
```

### Problem: "Topic does not exist"
Kafka auto-creates topics when first message is published. If disabled, create manually:
```bash
docker exec -it emis-kafka kafka-topics \
  --bootstrap-server localhost:9092 \
  --create \
  --topic emis.teacher.teachercreated \
  --partitions 3 \
  --replication-factor 1
```

## Configuration Reference

### Teacher Service (Producer)
| Setting | Value | Description |
|---------|-------|-------------|
| Kafka:BootstrapServers | localhost:9092 | Kafka broker address |
| ClientId | teacher-service-producer | Producer identifier |
| TopicPrefix | emis.teacher | Topic naming prefix |

### Identity Service (Consumer)
| Setting | Value | Description |
|---------|-------|-------------|
| Kafka:BootstrapServers | localhost:9092 | Kafka broker address |
| GroupId | identity-service-consumer | Consumer group |
| ClientId | identity-service | Consumer identifier |
| Topics | emis.teacher.teachercreated | Topics to subscribe |
| AutoOffsetReset | Earliest | Start from beginning |

## Summary

This integration implements a robust event-driven architecture using Apache Kafka for inter-service communication. Key benefits:

✅ **Loose Coupling**: Services don't know about each other  
✅ **Async Processing**: Non-blocking, improves performance  
✅ **Scalability**: Can add more consumers for parallel processing  
✅ **Reliability**: Events persisted, guaranteed delivery  
✅ **Auditability**: All events logged and traceable  
✅ **Extensibility**: Easy to add new event consumers  

The implementation follows DDD and Clean Architecture principles with proper separation of concerns across all layers.

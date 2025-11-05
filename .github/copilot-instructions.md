# EMIS Copilot Instructions

## Project Overview
Educational Management Information System (EMIS) for kindergartens using **Microservices + Clean Architecture + Domain-Driven Design**. Supports 25,000+ multi-tenant schools with 38M+ users.

## ⚠️ CRITICAL: Architecture Standards First

**This project prioritizes architectural integrity above all else:**

1. **Clean Architecture is NON-NEGOTIABLE**
   - Strict layer separation: Domain → Application → Infrastructure → API
   - Zero dependencies from inner layers to outer layers
   - All business logic MUST reside in Domain layer
   - Application layer orchestrates, NEVER contains business rules

2. **Domain-Driven Design is MANDATORY**
   - All domain concepts modeled as Aggregates, Entities, or Value Objects
   - Repository pattern with encapsulated methods (NO IQueryable leaks)
   - Domain events for cross-aggregate communication
   - Ubiquitous language in code matches business terminology

3. **Microservices Principles are ENFORCED**
   - Each service owns its database (no shared databases)
   - Inter-service communication via events (RabbitMQ) or gRPC only
   - No direct database access across services
   - Each service is independently deployable

**When in doubt**: Reference the Student service implementation as the gold standard. If a suggestion violates Clean Architecture or DDD principles, reject it - even if it seems "easier" or "faster".

## Architecture Principles

### Clean Architecture Layers (4-Layer Pattern)
Each microservice follows strict dependency rules: `Domain ← Application ← Infrastructure ← API`

1. **Domain Layer**: Pure business logic with DDD patterns
   - Entities inherit from `Entity` (EMIS.SharedKernel) with `Guid Id` and domain events
   - Aggregate Roots inherit from `TenantEntity` and implement `IAggregateRoot`
   - Value Objects inherit from `ValueObject` with `GetEqualityComponents()`
   - Repository interfaces define specific methods (NO `IQueryable` exposure per DDD best practices)

2. **Application Layer**: CQRS with MediatR
   - Commands return `ApiResponse<T>` (EMIS.BuildingBlocks.ApiResponse)
   - Queries return `ApiResponse<T>` or `ApiResponse<PagedResult<T>>`
   - Use FluentValidation for command validation
   - AutoMapper for entity-to-DTO mapping

3. **Infrastructure Layer**: Data access with EF Core
   - DbContext per service with `TenantId` global query filter
   - Repository implementations with encapsulated query methods (see `docs/DDD-Repository-Pattern-Best-Practices.md`)
   - Example: `GetPagedAsync(int pageNumber, int pageSize, string? searchTerm, ...)`

4. **API Layer**: REST endpoints
   - Controllers inject `IMediator`, send commands/queries
   - All responses wrapped in `ApiResponse<T>` structure
   - Serilog for logging

### Multi-Tenancy Pattern
- All domain entities inherit from `TenantEntity` (EMIS.BuildingBlocks.MultiTenant)
- `TenantId` is `Guid`, set in constructor, immutable
- Global EF Core query filter: `.HasQueryFilter(e => e.TenantId == _currentTenantId)`
- JWT tokens contain `TenantId` claim

### Domain-Driven Design Patterns

**Aggregate Example** (`Student.Domain.Aggregates.Student`):
```csharp
public class Student : TenantEntity, IAggregateRoot
{
    private readonly List<Parent> _parents = new();
    public StudentCode StudentCode { get; private set; } // Value Object
    
    // Constructor enforces invariants
    public Student(Guid tenantId, StudentCode code, string name, ...)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(...);
        ValidateAge(dateOfBirth); // Business rule
        // ... set properties
        AddDomainEvent(new StudentCreatedEvent(...)); // Domain event
    }
    
    // Business logic in methods
    public void AddParent(Parent parent) { /* validation */ }
}
```

**Value Object Example** (`StudentCode`):
```csharp
public class StudentCode : ValueObject
{
    public static StudentCode Create(string value) { /* validation */ }
    protected override IEnumerable<object> GetEqualityComponents() 
        => new[] { Value };
}
```

## Development Workflows

### Local Development Setup
```bash
# Start infrastructure (MySQL, MongoDB, Redis, RabbitMQ, MinIO, Elasticsearch)
docker-compose up -d

# Verify services (all should show "Up")
docker-compose ps

# Build solution (58 projects)
dotnet restore && dotnet build

# Run a service
cd src/Services/Student/Student.API
dotnet run  # https://localhost:5002/swagger
```

**Default Infrastructure Credentials**: `admin / EMISPassword123!`

### Creating a New Service Feature

1. **Domain**: Create aggregate/entity with business rules, add domain events
2. **Application**: Create command/query with validator, create handler
3. **Infrastructure**: Implement repository methods, add EF configurations
4. **API**: Add controller endpoint that sends command/query via `IMediator`

### Repository Anti-Pattern (CRITICAL - DDD VIOLATION)
❌ **NEVER** expose `IQueryable<T>` from repositories - violates DDD encapsulation

**This is a HARD RULE in this project. No exceptions.**

✅ **DO** create specific methods:
```csharp
Task<(IEnumerable<Student> Items, int TotalCount)> GetPagedAsync(
    int pageNumber, int pageSize, string? searchTerm, 
    StudentStatus? status, Guid? classId, CancellationToken ct);
```

**Rationale**: See `docs/DDD-Repository-Pattern-Best-Practices.md` for detailed explanation.

**Why this matters**: Exposing IQueryable breaks encapsulation, leaks infrastructure concerns to application layer, makes testing harder, and prevents true DDD implementation. This project values DDD principles over developer convenience.

## Project Structure Conventions

### Service Organization
```
Services/
├── Identity/          # Auth & JWT tokens
├── Student/           # Core service with full implementation
├── Teacher/
├── Attendance/        # Daily check-in
├── Assessment/
├── NewsFeed/          # School announcements
├── Chat/             # Real-time messaging (SignalR)
├── Payment/          # Tuition management
├── Menu/             # Daily meal plans
├── Leave/            # Absence requests
├── Camera/           # Surveillance streaming
├── Report/           # Analytics
└── Notification/     # Multi-channel alerts
```

### Naming Conventions
- **Commands**: `{Action}{Entity}Command` (e.g., `CreateStudentCommand`)
- **Queries**: `Get{Entity(s)}{Filter?}Query` (e.g., `GetStudentsByClassQuery`)
- **Handlers**: `{CommandOrQuery}Handler`
- **Events**: `{Entity}{Action}Event` (e.g., `StudentCreatedEvent`)
- **DTOs**: `{Entity}Dto`, `{Entity}DetailDto`
- **Value Objects**: Domain concept name (e.g., `StudentCode`, `Address`)

## Key Integration Points

### Event-Driven Communication
- **Integration Events** in `EMIS.EventBus` for cross-service communication
- Published via RabbitMQ (localhost:5672, UI: localhost:15672)
- Example: `StudentCreatedEvent` → Notification Service sends welcome message

### API Gateway
- **YARP** reverse proxy at `src/ApiGateway/`
- Routes requests to services based on path prefix (e.g., `/api/v1/students/*`)
- JWT validation at gateway level

### Database Strategy
- **MySQL** (port 3306): Transactional data for most services
- **MongoDB** (port 27017): Chat history, logs
- **Redis** (port 6379): Distributed cache, sessions

### Authentication Flow
1. Client → Identity Service (`/api/v1/auth/login`) with `tenantId`, credentials
2. Identity Service validates → returns JWT with `TenantId`, `UserId`, `Roles` claims
3. Subsequent requests include `Authorization: Bearer {token}`
4. Each service extracts `TenantId` from token for data isolation

## Testing Practices
- **Unit Tests**: Test domain logic (aggregates, value objects, business rules)
- **Integration Tests**: Test handlers with in-memory database
- Run: `dotnet test` from solution root

## Common Patterns

### ApiResponse Wrapper
All API responses use standardized format:
```csharp
return ApiResponse<StudentDto>.SuccessResult(dto);
return ApiResponse.ErrorResult("Not found", 404);
```

### Exception Handling
- `BusinessRuleValidationException`: Domain rule violations (EMIS.BuildingBlocks.Exceptions)
- `NotFoundException`: Entity not found
- Custom middleware converts to `ApiResponse` with appropriate status codes

### Pagination
Use `PagedResult<T>` from EMIS.BuildingBlocks.Pagination:
```csharp
var pagedResult = new PagedResult<StudentDto>(
    items, totalCount, pageNumber, pageSize);
```

## Service-Specific Notes

### Student Service (Gold Standard Reference Implementation)
- **ALWAYS reference** `src/Services/Student/` when implementing new features
- Demonstrates all DDD patterns correctly:
  - Aggregates with business rules: `Student` aggregate enforces age validation, parent requirements
  - Entities: `Parent`, `Class` with proper encapsulation
  - Value Objects: `StudentCode`, `Address` with immutability
  - Domain Events: `StudentCreatedEvent`, `StudentUpdatedEvent`
  - Repository encapsulation: `GetPagedAsync`, `GetStudentsWithParentsPagedAsync` (NO IQueryable!)
- Business logic lives in domain methods (`AddParent()`, `ChangeStatus()`, `ValidateAge()`)
- Application handlers orchestrate, don't contain business rules
- **When in conflict**: Student service implementation > any "quick fix" suggestion

## Configuration Management
- `appsettings.json`: Default config
- `appsettings.Development.json`: Local overrides
- Connection strings template: `Server=localhost;Port=3306;Database=emis_{service};...`

## Documentation References
- `docs/02-Microservices-Design.md`: Service boundaries and responsibilities
- `docs/03-Domain-Models-and-Database.md`: Database schemas per service
- `docs/DDD-Repository-Pattern-Best-Practices.md`: Why we avoid IQueryable in repos
- `QUICK_START.md`: Setup commands and examples

## Code Generation Scripts
- `scripts/create-services.sh`: Scaffolds new service with 4 layers
- `scripts/add-to-solution.sh`: Adds projects to EMIS.sln

When suggesting code, prioritize consistency with existing patterns in `Student` service implementation.

# EMIS Solution Structure

## 📁 Project Organization

```
EMIS.sln                                    # Main solution file
│
├── src/
│   ├── BuildingBlocks/                     # Shared libraries
│   │   ├── EMIS.SharedKernel/              # DDD base classes
│   │   │   ├── Entity.cs                   # Base entity
│   │   │   ├── AggregateRoot.cs            # Aggregate root base
│   │   │   ├── ValueObject.cs              # Value object base
│   │   │   ├── IDomainEvent.cs             # Domain event interface
│   │   │   ├── DomainEvent.cs              # Domain event base
│   │   │   ├── IRepository.cs              # Repository interface
│   │   │   └── IUnitOfWork.cs              # Unit of work interface
│   │   │
│   │   ├── EMIS.BuildingBlocks/            # Common utilities
│   │   │   ├── ApiResponse/                # Standard API response
│   │   │   ├── Pagination/                 # Pagination support
│   │   │   ├── Exceptions/                 # Custom exceptions
│   │   │   └── MultiTenant/                # Multi-tenancy support
│   │   │
│   │   └── EMIS.EventBus/                  # Event bus abstraction
│   │       ├── IIntegrationEvent.cs        # Integration event interface
│   │       ├── IntegrationEvent.cs         # Integration event base
│   │       ├── IEventBus.cs                # Event bus interface
│   │       └── IIntegrationEventHandler.cs # Event handler interface
│   │
│   ├── Services/                           # Microservices
│   │   ├── Identity/                       # Authentication & Authorization
│   │   │   ├── Identity.API/               # Web API
│   │   │   ├── Identity.Application/       # Use cases, commands, queries
│   │   │   ├── Identity.Domain/            # Domain models, aggregates
│   │   │   └── Identity.Infrastructure/    # Data access, external services
│   │   │
│   │   ├── Student/                        # Student Management
│   │   │   ├── Student.API/
│   │   │   ├── Student.Application/
│   │   │   ├── Student.Domain/
│   │   │   └── Student.Infrastructure/
│   │   │
│   │   ├── Teacher/                        # Teacher Management
│   │   │   ├── Teacher.API/
│   │   │   ├── Teacher.Application/
│   │   │   ├── Teacher.Domain/
│   │   │   └── Teacher.Infrastructure/
│   │   │
│   │   ├── Attendance/                     # Attendance & Daily Comments
│   │   │   ├── Attendance.API/
│   │   │   ├── Attendance.Application/
│   │   │   ├── Attendance.Domain/
│   │   │   └── Attendance.Infrastructure/
│   │   │
│   │   ├── Assessment/                     # Student Assessment
│   │   │   ├── Assessment.API/
│   │   │   ├── Assessment.Application/
│   │   │   ├── Assessment.Domain/
│   │   │   └── Assessment.Infrastructure/
│   │   │
│   │   ├── NewsFeed/                       # News Feed & Announcements
│   │   │   ├── NewsFeed.API/
│   │   │   ├── NewsFeed.Application/
│   │   │   ├── NewsFeed.Domain/
│   │   │   └── NewsFeed.Infrastructure/
│   │   │
│   │   ├── Chat/                           # Real-time Chat
│   │   │   ├── Chat.API/
│   │   │   ├── Chat.Application/
│   │   │   ├── Chat.Domain/
│   │   │   └── Chat.Infrastructure/
│   │   │
│   │   ├── Payment/                        # Payment & Invoicing
│   │   │   ├── Payment.API/
│   │   │   ├── Payment.Application/
│   │   │   ├── Payment.Domain/
│   │   │   └── Payment.Infrastructure/
│   │   │
│   │   ├── Menu/                           # Daily Menu Management
│   │   │   ├── Menu.API/
│   │   │   ├── Menu.Application/
│   │   │   ├── Menu.Domain/
│   │   │   └── Menu.Infrastructure/
│   │   │
│   │   ├── Leave/                          # Leave Management
│   │   │   ├── Leave.API/
│   │   │   ├── Leave.Application/
│   │   │   ├── Leave.Domain/
│   │   │   └── Leave.Infrastructure/
│   │   │
│   │   ├── Camera/                         # Camera Surveillance
│   │   │   ├── Camera.API/
│   │   │   ├── Camera.Application/
│   │   │   ├── Camera.Domain/
│   │   │   └── Camera.Infrastructure/
│   │   │
│   │   ├── Report/                         # Reporting & Analytics
│   │   │   ├── Report.API/
│   │   │   ├── Report.Application/
│   │   │   ├── Report.Domain/
│   │   │   └── Report.Infrastructure/
│   │   │
│   │   └── Notification/                   # Notification Service
│   │       ├── Notification.API/
│   │       ├── Notification.Application/
│   │       ├── Notification.Domain/
│   │       └── Notification.Infrastructure/
│   │
│   └── ApiGateway/                         # YARP API Gateway
│       └── ApiGateway/
│
├── tests/
│   ├── UnitTests/                          # Unit tests
│   └── IntegrationTests/                   # Integration tests
│
├── scripts/                                # Utility scripts
│   ├── create-services.sh                  # Script to create services
│   └── add-to-solution.sh                  # Script to add projects to solution
│
├── docs/                                   # Documentation
│   ├── 01-System-Overview.md
│   ├── 02-Microservices-Design.md
│   ├── 03-Domain-Models-and-Database.md
│   ├── 04-API-Contracts.md
│   ├── 05-Technology-Stack.md
│   └── 06-Deployment-Architecture.md
│
├── docker-compose.yml                      # Docker infrastructure
├── .gitignore
└── README.md
```

## 🏗️ Clean Architecture Layers

Mỗi microservice được tổ chức theo **Clean Architecture** với 4 layers:

### 1. Domain Layer (`*.Domain`)
**Trách nhiệm:** Core business logic, domain models

**Nội dung:**
- **Entities:** Domain entities với business logic
- **Aggregates:** Aggregate roots (entry point to aggregate)
- **Value Objects:** Immutable objects defined by attributes
- **Domain Events:** Events that occurred in domain
- **Domain Services:** Business logic không thuộc entity
- **Repository Interfaces:** Abstraction cho data access
- **Specifications:** Query specifications (optional)

**Dependencies:** EMIS.SharedKernel (minimal dependencies)

**Example structure:**
```
Student.Domain/
├── Entities/
│   ├── Student.cs              # Aggregate Root
│   └── Parent.cs               # Entity
├── ValueObjects/
│   ├── Address.cs
│   └── StudentCode.cs
├── Events/
│   ├── StudentCreatedEvent.cs
│   └── StudentStatusChangedEvent.cs
├── Repositories/
│   └── IStudentRepository.cs
├── Services/
│   └── StudentDomainService.cs
└── Enums/
    └── StudentStatus.cs
```

### 2. Application Layer (`*.Application`)
**Trách nhiệm:** Use cases, application logic, orchestration

**Nội dung:**
- **Commands:** Write operations (CQRS)
- **Queries:** Read operations (CQRS)
- **Command Handlers:** Handle commands with MediatR
- **Query Handlers:** Handle queries with MediatR
- **DTOs:** Data transfer objects
- **View Models:** Response models
- **Validators:** FluentValidation
- **Mapping:** AutoMapper profiles
- **Application Services:** Orchestrate domain logic
- **Integration Event Handlers:** Handle events from other services

**Dependencies:** Domain, EMIS.BuildingBlocks, MediatR, FluentValidation, AutoMapper

**Example structure:**
```
Student.Application/
├── Commands/
│   ├── CreateStudent/
│   │   ├── CreateStudentCommand.cs
│   │   ├── CreateStudentCommandHandler.cs
│   │   └── CreateStudentCommandValidator.cs
│   └── UpdateStudent/
│       ├── UpdateStudentCommand.cs
│       ├── UpdateStudentCommandHandler.cs
│       └── UpdateStudentCommandValidator.cs
├── Queries/
│   ├── GetStudentById/
│   │   ├── GetStudentByIdQuery.cs
│   │   └── GetStudentByIdQueryHandler.cs
│   └── GetStudentsList/
│       ├── GetStudentsListQuery.cs
│       └── GetStudentsListQueryHandler.cs
├── DTOs/
│   ├── StudentDto.cs
│   └── ParentDto.cs
├── ViewModels/
│   └── StudentViewModel.cs
├── Mappings/
│   └── StudentProfile.cs
├── IntegrationEvents/
│   ├── Events/
│   │   └── StudentCreatedIntegrationEvent.cs
│   └── Handlers/
│       └── UserRegisteredEventHandler.cs
└── Behaviors/
    ├── ValidationBehavior.cs
    └── LoggingBehavior.cs
```

### 3. Infrastructure Layer (`*.Infrastructure`)
**Trách nhiệm:** External concerns, data persistence, external services

**Nội dung:**
- **DbContext:** Entity Framework Core
- **Repositories:** Repository implementations
- **Migrations:** Database migrations
- **EntityConfigurations:** EF Core entity mappings
- **External Services:** HTTP clients, email, SMS
- **Caching:** Redis cache implementation
- **File Storage:** MinIO/S3 integration
- **Message Bus:** RabbitMQ/MassTransit implementation

**Dependencies:** Domain, Application, EF Core, Dapper, Redis, MassTransit

**Example structure:**
```
Student.Infrastructure/
├── Persistence/
│   ├── ApplicationDbContext.cs
│   ├── Repositories/
│   │   └── StudentRepository.cs
│   ├── Configurations/
│   │   ├── StudentConfiguration.cs
│   │   └── ParentConfiguration.cs
│   └── Migrations/
│       └── 20251105_InitialCreate.cs
├── Services/
│   ├── FileStorageService.cs
│   └── EmailService.cs
├── Caching/
│   └── RedisCacheService.cs
├── MessageBus/
│   └── EventBusService.cs
└── Extensions/
    └── ServiceCollectionExtensions.cs
```

### 4. Presentation Layer (`*.API`)
**Trách nhiệm:** HTTP endpoints, API controllers, SignalR hubs

**Nội dung:**
- **Controllers:** REST API endpoints
- **SignalR Hubs:** Real-time communication
- **gRPC Services:** Internal service communication
- **Middleware:** Custom middleware
- **Filters:** Action filters, exception filters
- **Configuration:** appsettings.json, Startup.cs

**Dependencies:** Application, Infrastructure

**Example structure:**
```
Student.API/
├── Controllers/
│   ├── StudentsController.cs
│   └── ParentsController.cs
├── Hubs/
│   └── NotificationHub.cs (optional)
├── gRPC/
│   └── StudentGrpcService.cs
├── Middleware/
│   ├── TenantMiddleware.cs
│   └── ExceptionHandlingMiddleware.cs
├── Filters/
│   └── ValidateModelFilter.cs
├── Extensions/
│   └── ServiceCollectionExtensions.cs
├── appsettings.json
├── appsettings.Development.json
├── Program.cs
└── Dockerfile
```

## 📦 Shared Libraries

### EMIS.SharedKernel
**Purpose:** DDD base classes, shared by all domain layers

**Contents:**
- `Entity.cs` - Base entity class
- `AggregateRoot.cs` - Aggregate root base
- `ValueObject.cs` - Value object base
- `IDomainEvent.cs`, `DomainEvent.cs` - Domain events
- `IRepository.cs` - Repository pattern
- `IUnitOfWork.cs` - Unit of work pattern

### EMIS.BuildingBlocks
**Purpose:** Common utilities, cross-cutting concerns

**Contents:**
- `ApiResponse/` - Standard API responses
- `Pagination/` - Pagination support
- `Exceptions/` - Custom exceptions
- `MultiTenant/` - Multi-tenancy utilities

### EMIS.EventBus
**Purpose:** Event-driven communication abstraction

**Contents:**
- `IIntegrationEvent.cs` - Integration event interface
- `IntegrationEvent.cs` - Base integration event
- `IEventBus.cs` - Event bus interface
- `IIntegrationEventHandler.cs` - Event handler interface

## 🚀 Getting Started

### Prerequisites
```bash
# Check .NET version
dotnet --version  # Should be 8.0 or higher

# Check Docker
docker --version
docker-compose --version
```

### Build Solution
```bash
# Restore dependencies
dotnet restore

# Build all projects
dotnet build

# Run tests
dotnet test
```

### Run Infrastructure
```bash
# Start all infrastructure services
docker-compose up -d

# Check services
docker-compose ps

# View logs
docker-compose logs -f

# Stop all services
docker-compose down
```

### Run Individual Service
```bash
# Run Student Service
cd src/Services/Student/Student.API
dotnet run

# Access Swagger UI
# http://localhost:5001/swagger
```

## 📊 Current Status

✅ **Completed:**
- [x] Solution structure created
- [x] 13 microservices scaffolded (4 layers each)
- [x] BuildingBlocks libraries with DDD patterns
- [x] SharedKernel with base classes
- [x] EventBus abstraction
- [x] Docker Compose infrastructure
- [x] All projects added to solution
- [x] Successfully built (58 projects)

🚧 **Next Steps:**
1. Implement domain models for each service
2. Add EF Core configurations and migrations
3. Implement repository patterns
4. Add MediatR commands/queries
5. Configure dependency injection
6. Add authentication/authorization
7. Implement API controllers
8. Add integration tests
9. Configure API Gateway (YARP)
10. Add monitoring and logging

## 📚 Additional Resources

- [Main Documentation](../README.md)
- [System Overview](../docs/01-System-Overview.md)
- [Microservices Design](../docs/02-Microservices-Design.md)
- [Domain Models & Database](../docs/03-Domain-Models-and-Database.md)
- [API Contracts](../docs/04-API-Contracts.md)
- [Technology Stack](../docs/05-Technology-Stack.md)
- [Deployment Architecture](../docs/06-Deployment-Architecture.md)

## 🤝 Development Guidelines

### Naming Conventions
- **Projects:** `ServiceName.LayerName` (e.g., `Student.Domain`)
- **Namespaces:** Match folder structure
- **Classes:** PascalCase
- **Methods:** PascalCase
- **Variables:** camelCase
- **Constants:** UPPER_CASE

### Git Workflow
```bash
# Create feature branch
git checkout -b feature/student-service-domain

# Commit changes
git add .
git commit -m "feat: implement Student aggregate"

# Push to remote
git push origin feature/student-service-domain

# Create Pull Request
```

### Commit Message Format
```
<type>(<scope>): <subject>

Examples:
feat(student): add Student aggregate root
fix(api): resolve null reference exception
docs: update API documentation
test(student): add unit tests for Student
refactor(infrastructure): improve repository pattern
```

---

**Built with ❤️ by EMIS Team**

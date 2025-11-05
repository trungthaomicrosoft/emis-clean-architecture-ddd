# Technology Stack & Tools

## 🎯 Core Technologies

### Backend Framework
- **.NET 8.0** - Latest LTS version
- **ASP.NET Core Web API** - RESTful services
- **ASP.NET Core gRPC** - Internal service communication
- **SignalR** - Real-time communication (Chat, Notifications)

### Programming Language
- **C# 12** - Latest features

---

## 🏗️ Architecture & Patterns

### Clean Architecture
- **Domain Layer** - Pure business logic
- **Application Layer** - Use cases, CQRS
- **Infrastructure Layer** - External concerns
- **Presentation Layer** - API, gRPC, SignalR

### Design Patterns
- **Domain-Driven Design (DDD)**
  - Aggregates
  - Entities
  - Value Objects
  - Domain Events
  - Repository Pattern
- **CQRS (Command Query Responsibility Segregation)**
  - MediatR for commands and queries
- **Event-Driven Architecture**
- **Saga Pattern** - For distributed transactions
- **Circuit Breaker Pattern** - Polly

---

## 📦 NuGet Packages by Layer

### Domain Layer (Minimal Dependencies)
```xml
<PackageReference Include="MediatR" Version="12.2.0" />
```

### Application Layer
```xml
<PackageReference Include="MediatR" Version="12.2.0" />
<PackageReference Include="FluentValidation" Version="11.9.0" />
<PackageReference Include="FluentValidation.DependencyInjectionExtensions" Version="11.9.0" />
<PackageReference Include="AutoMapper" Version="12.0.1" />
<PackageReference Include="AutoMapper.Extensions.Microsoft.DependencyInjection" Version="12.0.1" />
```

### Infrastructure Layer
```xml
<!-- ORM & Database -->
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.0" />
<PackageReference Include="Pomelo.EntityFrameworkCore.MySql" Version="8.0.0" />
<PackageReference Include="MongoDB.Driver" Version="2.23.0" />
<PackageReference Include="Dapper" Version="2.1.28" />

<!-- Caching -->
<PackageReference Include="Microsoft.Extensions.Caching.StackExchangeRedis" Version="8.0.0" />
<PackageReference Include="StackExchange.Redis" Version="2.7.10" />

<!-- Message Bus -->
<PackageReference Include="RabbitMQ.Client" Version="6.8.1" />
<PackageReference Include="MassTransit" Version="8.1.3" />
<PackageReference Include="MassTransit.RabbitMQ" Version="8.1.3" />

<!-- Storage -->
<PackageReference Include="Minio" Version="6.0.2" />

<!-- Authentication & Authorization -->
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.0" />
<PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="7.0.3" />
<PackageReference Include="Duende.IdentityServer" Version="7.0.0" />

<!-- Resilience -->
<PackageReference Include="Polly" Version="8.2.0" />
<PackageReference Include="Polly.Extensions.Http" Version="3.0.0" />
```

### Presentation Layer (API)
```xml
<PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="8.0.0" />
<PackageReference Include="Swashbuckle.AspNetCore" Version="6.5.0" />
<PackageReference Include="Microsoft.AspNetCore.SignalR" Version="8.0.0" />
<PackageReference Include="Grpc.AspNetCore" Version="2.60.0" />
<PackageReference Include="Serilog.AspNetCore" Version="8.0.0" />
<PackageReference Include="Serilog.Sinks.Console" Version="5.0.1" />
<PackageReference Include="Serilog.Sinks.Elasticsearch" Version="10.0.0" />
<PackageReference Include="HealthChecks.UI.Client" Version="8.0.0" />
<PackageReference Include="AspNetCore.HealthChecks.MySql" Version="8.0.0" />
<PackageReference Include="AspNetCore.HealthChecks.MongoDb" Version="8.0.0" />
<PackageReference Include="AspNetCore.HealthChecks.Redis" Version="8.0.0" />
<PackageReference Include="AspNetCore.HealthChecks.Rabbitmq" Version="8.0.0" />
```

---

## 🗄️ Databases

### Relational Database - MySQL 8.0+
**Usage:**
- Student Service
- Teacher Service
- Attendance Service
- Assessment Service
- Payment Service
- Menu Service
- Leave Service
- Camera Service
- Identity Service (can use separate MySQL)

**Why MySQL:**
- ✅ Free, open-source
- ✅ ACID compliance
- ✅ Good performance for transactional data
- ✅ Wide adoption and community support
- ✅ Compatible with Pomelo.EntityFrameworkCore.MySql

**Connection:**
```csharp
services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString),
        mysqlOptions => {
            mysqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null
            );
        }
    )
);
```

### NoSQL Database - MongoDB 6.0+
**Usage:**
- Chat Service (messages, conversations)
- News Feed Service (posts, comments)
- Notification Service
- Audit Logs
- Application Logs

**Why MongoDB:**
- ✅ Flexible schema for chat messages
- ✅ Excellent for high-write scenarios
- ✅ Built-in replication
- ✅ Horizontal scaling
- ✅ Rich query capabilities

**Connection:**
```csharp
services.AddSingleton<IMongoClient>(sp =>
{
    var settings = MongoClientSettings.FromConnectionString(connectionString);
    settings.MaxConnectionPoolSize = 500;
    return new MongoClient(settings);
});

services.AddScoped(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    return client.GetDatabase(databaseName);
});
```

### Cache - Redis 7.0+
**Usage:**
- Session management
- JWT token blacklist
- Real-time data (online users)
- Cache frequently accessed data
- Rate limiting
- Distributed locks

**Why Redis:**
- ✅ In-memory, extremely fast
- ✅ Pub/Sub capabilities
- ✅ Data structures (String, Hash, List, Set, Sorted Set)
- ✅ TTL support
- ✅ Persistence options

**Connection:**
```csharp
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = configuration["Redis:ConnectionString"];
    options.InstanceName = "EMIS_";
});

services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(configuration["Redis:ConnectionString"])
);
```

---

## 📦 Message Bus & Event Streaming

### RabbitMQ 3.12+
**Usage:**
- Domain Events
- Integration Events
- Asynchronous communication between services
- Event-driven workflows

**Why RabbitMQ:**
- ✅ Reliable message delivery
- ✅ Flexible routing (Direct, Topic, Fanout, Headers)
- ✅ Dead letter queues
- ✅ Message persistence
- ✅ Management UI

**MassTransit Configuration:**
```csharp
services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();
    
    x.AddConsumersFromNamespaceContaining<StudentCreatedConsumer>();
    
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(configuration["RabbitMQ:Host"], h =>
        {
            h.Username(configuration["RabbitMQ:Username"]);
            h.Password(configuration["RabbitMQ:Password"]);
        });
        
        cfg.ConfigureEndpoints(context);
    });
});
```

### Apache Kafka (Alternative for high-throughput scenarios)
**When to consider:**
- Need to process millions of events per day
- Event streaming analytics
- Event sourcing with long-term storage

---

## 🗂️ File Storage

### MinIO (S3-compatible)
**Usage:**
- Student photos
- Teacher documents
- Assessment media (images, videos)
- Chat attachments
- Daily comment photos/videos
- Camera recordings
- Report files

**Why MinIO:**
- ✅ Self-hosted, open-source
- ✅ S3-compatible API
- ✅ High performance
- ✅ Distributed storage
- ✅ On-premise deployment friendly

**Configuration:**
```csharp
services.AddSingleton<IMinioClient>(sp =>
{
    return new MinioClient()
        .WithEndpoint(configuration["MinIO:Endpoint"])
        .WithCredentials(
            configuration["MinIO:AccessKey"],
            configuration["MinIO:SecretKey"]
        )
        .WithSSL(false) // Set true for production
        .Build();
});
```

**Bucket Structure:**
```
emis-tenant-{tenantId}-students
emis-tenant-{tenantId}-teachers
emis-tenant-{tenantId}-assessments
emis-tenant-{tenantId}-chat
emis-tenant-{tenantId}-comments
emis-tenant-{tenantId}-cameras
```

---

## 🔐 Authentication & Authorization

### JWT (JSON Web Tokens)
**Libraries:**
- `System.IdentityModel.Tokens.Jwt`
- `Microsoft.AspNetCore.Authentication.JwtBearer`

**Token Structure:**
```json
{
  "sub": "user_id",
  "tenant_id": "school-abc-xyz",
  "name": "Nguyễn Văn A",
  "email": "user@example.com",
  "roles": ["Teacher", "ClassTeacher"],
  "permissions": ["student.view", "attendance.create"],
  "exp": 1699200000,
  "iat": 1699196400
}
```

### Duende IdentityServer (Optional)
**For advanced scenarios:**
- OAuth2 / OpenID Connect
- Single Sign-On (SSO)
- API scopes
- External identity providers

---

## 🌐 API Gateway

### YARP (Yet Another Reverse Proxy)
**Why YARP:**
- ✅ Built by Microsoft
- ✅ High performance
- ✅ .NET native
- ✅ Configuration-based
- ✅ Middleware extensibility

**Features:**
- Request routing
- Load balancing
- Rate limiting
- Authentication/Authorization
- Request/Response transformation
- Health checks

**Configuration:**
```json
{
  "ReverseProxy": {
    "Routes": {
      "student-route": {
        "ClusterId": "student-cluster",
        "Match": {
          "Path": "/api/v1/students/{**catch-all}"
        }
      }
    },
    "Clusters": {
      "student-cluster": {
        "Destinations": {
          "destination1": {
            "Address": "http://student-service:80"
          }
        }
      }
    }
  }
}
```

### Alternative: Ocelot
**Simpler option for smaller deployments**

---

## 📹 Video Streaming

### WebRTC
**For live camera streaming:**
- Peer-to-peer connection
- Low latency
- Browser support

### HLS (HTTP Live Streaming)
**For playback:**
- Works over HTTP
- Adaptive bitrate
- CDN-friendly

### Media Server Options:
1. **Kurento Media Server** (WebRTC)
2. **Janus Gateway** (WebRTC)
3. **FFmpeg** (Transcoding)
4. **OBS Studio** (Streaming)

---

## 📊 Logging & Monitoring

### Logging - Serilog
**Sinks:**
- Console (Development)
- Elasticsearch (Production)
- File (Fallback)

**Configuration:**
```csharp
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "EMIS")
    .Enrich.WithProperty("TenantId", tenantId)
    .WriteTo.Console()
    .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(elasticsearchUrl))
    {
        IndexFormat = $"emis-logs-{DateTime.UtcNow:yyyy-MM}",
        AutoRegisterTemplate = true
    })
    .CreateLogger();
```

### ELK Stack
- **Elasticsearch** - Log storage and indexing
- **Logstash** - Log processing (optional)
- **Kibana** - Visualization and dashboards

### Metrics - Prometheus + Grafana
**Libraries:**
- `prometheus-net`
- `prometheus-net.AspNetCore`

**Metrics to track:**
- Request rate
- Response time
- Error rate
- Database query performance
- Cache hit ratio
- Queue length

### Distributed Tracing - Jaeger / OpenTelemetry
**Library:**
- `OpenTelemetry`
- `OpenTelemetry.Instrumentation.AspNetCore`
- `OpenTelemetry.Instrumentation.EntityFrameworkCore`

**Trace:**
- Request flow across services
- Service dependencies
- Performance bottlenecks

---

## 🧪 Testing

### Unit Testing
- **xUnit** - Test framework
- **FluentAssertions** - Assertion library
- **Moq** - Mocking framework
- **AutoFixture** - Test data generation

### Integration Testing
- **WebApplicationFactory** - ASP.NET Core testing
- **Testcontainers** - Docker containers for testing
- **Respawn** - Database cleanup

### Load Testing
- **k6** - Load testing tool
- **JMeter** - Performance testing

### Testing Libraries:
```xml
<PackageReference Include="xunit" Version="2.6.2" />
<PackageReference Include="xunit.runner.visualstudio" Version="2.5.4" />
<PackageReference Include="FluentAssertions" Version="6.12.0" />
<PackageReference Include="Moq" Version="4.20.70" />
<PackageReference Include="AutoFixture" Version="4.18.1" />
<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.0.0" />
<PackageReference Include="Testcontainers" Version="3.6.0" />
<PackageReference Include="Respawn" Version="6.1.0" />
```

---

## 📱 Mobile Development

### Flutter 3.16+
**Why Flutter:**
- ✅ Single codebase for iOS & Android
- ✅ Fast development (Hot Reload)
- ✅ Beautiful UI components
- ✅ Good performance
- ✅ Large community

**Key Packages:**
```yaml
dependencies:
  # HTTP Client
  dio: ^5.4.0
  
  # State Management
  flutter_bloc: ^8.1.3
  provider: ^6.1.1
  
  # Navigation
  go_router: ^13.0.0
  
  # Local Storage
  shared_preferences: ^2.2.2
  hive: ^2.2.3
  
  # Authentication
  flutter_secure_storage: ^9.0.0
  
  # Real-time
  socket_io_client: ^2.0.3
  signalr_netcore: ^1.3.6
  
  # Media
  image_picker: ^1.0.7
  video_player: ^2.8.2
  cached_network_image: ^3.3.1
  
  # Notifications
  firebase_messaging: ^14.7.9
  flutter_local_notifications: ^16.3.0
  
  # UI
  flutter_svg: ^2.0.9
  shimmer: ^3.0.0
```

---

## 💻 Web Development

### Blazor Server / React / Angular

#### Option 1: Blazor Server (Recommended for .NET teams)
**Pros:**
- ✅ C# end-to-end
- ✅ Component-based
- ✅ Server-side rendering
- ✅ Real-time with SignalR (built-in)

#### Option 2: React 18+
**Pros:**
- ✅ Popular, large ecosystem
- ✅ Component-based
- ✅ Virtual DOM
- ✅ Good for complex UIs

**Key Libraries:**
```json
{
  "dependencies": {
    "react": "^18.2.0",
    "react-dom": "^18.2.0",
    "react-router-dom": "^6.20.0",
    "axios": "^1.6.2",
    "react-query": "^3.39.3",
    "@tanstack/react-table": "^8.10.7",
    "@microsoft/signalr": "^8.0.0",
    "zustand": "^4.4.7",
    "tailwindcss": "^3.3.6",
    "chart.js": "^4.4.0",
    "react-chartjs-2": "^5.2.0"
  }
}
```

#### Option 3: Angular 17+
**Pros:**
- ✅ Full-featured framework
- ✅ TypeScript native
- ✅ Dependency injection
- ✅ RxJS for reactive programming

---

## 🐳 Containerization & Orchestration

### Docker
**Dockerfile Example (Student Service):**
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Services/Student/Student.API/Student.API.csproj", "Services/Student/Student.API/"]
COPY ["src/Services/Student/Student.Application/Student.Application.csproj", "Services/Student/Student.Application/"]
COPY ["src/Services/Student/Student.Domain/Student.Domain.csproj", "Services/Student/Student.Domain/"]
COPY ["src/Services/Student/Student.Infrastructure/Student.Infrastructure.csproj", "Services/Student/Student.Infrastructure/"]
RUN dotnet restore "Services/Student/Student.API/Student.API.csproj"
COPY . .
WORKDIR "/src/Services/Student/Student.API"
RUN dotnet build "Student.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Student.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Student.API.dll"]
```

### Kubernetes (K8s)
**Deployment Example:**
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: student-service
spec:
  replicas: 3
  selector:
    matchLabels:
      app: student-service
  template:
    metadata:
      labels:
        app: student-service
    spec:
      containers:
      - name: student-service
        image: emis/student-service:latest
        ports:
        - containerPort: 80
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: ConnectionStrings__DefaultConnection
          valueFrom:
            secretKeyRef:
              name: student-db-secret
              key: connection-string
        resources:
          requests:
            memory: "256Mi"
            cpu: "250m"
          limits:
            memory: "512Mi"
            cpu: "500m"
        livenessProbe:
          httpGet:
            path: /health/live
            port: 80
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /health/ready
            port: 80
          initialDelaySeconds: 10
          periodSeconds: 5
```

### Helm Charts
**For managing Kubernetes deployments:**
- Templating
- Version control
- Easy rollbacks

---

## 🔧 Development Tools

### IDE
- **Visual Studio 2022** (Windows)
- **Visual Studio Code** (Cross-platform)
- **JetBrains Rider** (Cross-platform)

### VS Code Extensions
- C# Dev Kit
- Docker
- Kubernetes
- REST Client
- MongoDB for VS Code
- GitLens

### Database Tools
- **MySQL Workbench** - MySQL management
- **MongoDB Compass** - MongoDB GUI
- **Redis Insight** - Redis GUI
- **DBeaver** - Universal database tool

### API Testing
- **Postman** - API testing
- **Swagger UI** - Interactive API docs
- **REST Client** (VS Code extension)

---

## 🚀 CI/CD

### GitLab CI / GitHub Actions

**GitLab CI Example (.gitlab-ci.yml):**
```yaml
stages:
  - build
  - test
  - publish
  - deploy

variables:
  DOCKER_REGISTRY: registry.emis.com
  IMAGE_NAME: student-service

build:
  stage: build
  image: mcr.microsoft.com/dotnet/sdk:8.0
  script:
    - dotnet restore
    - dotnet build --configuration Release
  artifacts:
    paths:
      - ./bin/Release/

test:
  stage: test
  image: mcr.microsoft.com/dotnet/sdk:8.0
  script:
    - dotnet test --configuration Release --no-build

publish:
  stage: publish
  image: docker:latest
  services:
    - docker:dind
  script:
    - docker build -t $DOCKER_REGISTRY/$IMAGE_NAME:$CI_COMMIT_SHORT_SHA .
    - docker push $DOCKER_REGISTRY/$IMAGE_NAME:$CI_COMMIT_SHORT_SHA
  only:
    - main

deploy:
  stage: deploy
  image: bitnami/kubectl:latest
  script:
    - kubectl set image deployment/student-service student-service=$DOCKER_REGISTRY/$IMAGE_NAME:$CI_COMMIT_SHORT_SHA
  only:
    - main
```

---

## 📋 Code Quality & Security

### Static Code Analysis
- **SonarQube** - Code quality & security
- **Roslyn Analyzers** - .NET code analysis
- **StyleCop** - Code style

### Security Scanning
- **OWASP Dependency-Check** - Vulnerability scanning
- **Snyk** - Dependency vulnerability
- **Trivy** - Container scanning

### Code Coverage
- **Coverlet** - .NET code coverage
- **ReportGenerator** - Coverage reports

---

## 🔐 Security Best Practices

### Application Level
- ✅ Input validation (FluentValidation)
- ✅ Output encoding
- ✅ Parameterized queries (EF Core, Dapper)
- ✅ HTTPS everywhere
- ✅ CORS configuration
- ✅ Rate limiting
- ✅ API key validation

### Authentication & Authorization
- ✅ JWT with short expiration
- ✅ Refresh token rotation
- ✅ Password hashing (BCrypt/Argon2)
- ✅ Multi-factor authentication (optional)
- ✅ Role-based access control (RBAC)
- ✅ Permission-based authorization

### Data Security
- ✅ Encryption at rest (database encryption)
- ✅ Encryption in transit (TLS/SSL)
- ✅ Sensitive data masking in logs
- ✅ PII data protection
- ✅ Regular backups
- ✅ Audit logging

---

## 📊 Performance Optimization

### Caching Strategy
- **Response Caching** - HTTP caching headers
- **Distributed Cache** - Redis
- **In-Memory Cache** - IMemoryCache for local cache
- **Output Caching** - For static responses

### Database Optimization
- **Indexing** - Proper database indexes
- **Query Optimization** - EF Core query analysis
- **Connection Pooling** - Connection string configuration
- **Read Replicas** - For read-heavy workloads

### API Optimization
- **Compression** - Response compression (Gzip, Brotli)
- **Pagination** - Limit result sets
- **Partial Responses** - Return only requested fields
- **Batching** - Batch API requests

---

## 📚 Documentation

### API Documentation
- **Swagger/OpenAPI** - Interactive API docs
- **Redoc** - Alternative API documentation

### Architecture Documentation
- **C4 Model** - Architecture diagrams
- **PlantUML** - Diagram as code
- **Mermaid** - Flowcharts in Markdown

### Code Documentation
- **XML Comments** - .NET XML documentation
- **DocFX** - .NET documentation generator

---

## 🎓 Learning Resources

### Official Documentation
- [.NET Documentation](https://docs.microsoft.com/dotnet)
- [ASP.NET Core](https://docs.microsoft.com/aspnet/core)
- [Entity Framework Core](https://docs.microsoft.com/ef/core)
- [SignalR](https://docs.microsoft.com/aspnet/core/signalr)

### Books
- "Clean Architecture" - Robert C. Martin
- "Domain-Driven Design" - Eric Evans
- "Microservices Patterns" - Chris Richardson
- "Building Microservices" - Sam Newman

---

**Next:** [06-Deployment-Architecture.md](./06-Deployment-Architecture.md)

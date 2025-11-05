# EMIS - Educational Management Information System

## 📚 Documentation Index

Hệ thống quản lý trường mầm non theo kiến trúc **Microservices + Clean Architecture + Domain-Driven Design**

---

## 📖 Tài Liệu Thiết Kế

### 1. [System Overview](./01-System-Overview.md)
**Tổng quan hệ thống**
- Giới thiệu dự án và mục tiêu
- Quy mô hệ thống (25,000 trường, ~38 triệu users)
- Chức năng chính
- Kiến trúc tổng quan
- Multi-tenancy strategy
- Non-functional requirements
- Roadmap phát triển

### 2. [Microservices Design](./02-Microservices-Design.md)
**Thiết kế chi tiết từng microservice**
- 13 microservices với bounded context rõ ràng
- Domain models, responsibilities
- API endpoints
- Events (published/consumed)
- Technology stack cho từng service
- Communication patterns (gRPC, RabbitMQ)

**Danh sách Services:**
1. Identity Service - Authentication & Authorization
2. Student Service - Quản lý học sinh
3. Teacher Service - Quản lý giáo viên
4. Attendance Service - Điểm danh & nhận xét
5. Assessment Service - Đánh giá học sinh
6. News Feed Service - Bảng tin
7. Chat Service - Real-time messaging
8. Payment Service - Quản lý học phí
9. Menu Service - Thực đơn hàng ngày
10. Leave Service - Xin nghỉ phép
11. Camera Service - Giám sát camera
12. Report Service - Báo cáo & thống kê
13. Notification Service - Thông báo đa kênh

### 3. [Domain Models & Database Schema](./03-Domain-Models-and-Database.md)
**Thiết kế database chi tiết**
- Database strategy (MySQL, MongoDB, Redis)
- Table schemas cho tất cả services
- Indexes & optimization
- Multi-tenant data isolation
- Event sourcing (optional)
- Caching strategy

### 4. [API Contracts](./04-API-Contracts.md)
**Định nghĩa API endpoints**
- REST API specs cho 13 services
- Request/Response examples
- Authentication flow
- SignalR hubs (Chat, Notification)
- Common response format
- Error handling

### 5. [Technology Stack](./05-Technology-Stack.md)
**Công nghệ & tools sử dụng**
- .NET 8.0, ASP.NET Core, SignalR
- Clean Architecture & DDD patterns
- NuGet packages
- Databases (MySQL, MongoDB, Redis)
- Message bus (RabbitMQ/Kafka)
- File storage (MinIO)
- API Gateway (YARP)
- Monitoring (Prometheus, Grafana, ELK)
- Testing frameworks
- Mobile (Flutter) & Web (Blazor/React)

### 6. [Deployment Architecture](./06-Deployment-Architecture.md)
**Kiến trúc triển khai Kubernetes**
- Kubernetes cluster setup (on-premise)
- Hardware requirements
- Infrastructure deployment
- Microservices deployment manifests
- Secrets management
- Monitoring stack
- Backup & disaster recovery
- Scaling strategy
- Operational commands

---

## 🎯 Đặc Điểm Nổi Bật

### ✅ Kiến Trúc
- **Microservices Architecture** - Loosely coupled, independently deployable
- **Clean Architecture** - 4 layers (Domain, Application, Infrastructure, Presentation)
- **Domain-Driven Design** - Bounded contexts, aggregates, domain events
- **CQRS Pattern** - Command/Query separation với MediatR
- **Event-Driven** - Asynchronous communication

### ✅ Scalability
- **Multi-tenant** - Hỗ trợ 25,000 trường học
- **Horizontal Scaling** - Kubernetes HPA, cluster autoscaling
- **Database Sharding** - Khi cần thiết
- **Load Balancing** - HAProxy/Nginx + K8s services
- **Caching** - Redis distributed cache

### ✅ Security
- **JWT Authentication** - Access token + Refresh token
- **Role-Based Access Control** (RBAC)
- **Permission-Based Authorization**
- **Data Encryption** - At rest & in transit
- **Multi-tenant Isolation** - Data isolation per tenant
- **API Rate Limiting**

### ✅ Reliability
- **High Availability** - 3 master nodes, multiple replicas
- **Circuit Breaker** - Polly for resilience
- **Health Checks** - Liveness & readiness probes
- **Automated Backups** - Daily database backups
- **Disaster Recovery** - Velero cluster backups
- **Monitoring & Alerting** - 24/7 observability

### ✅ Performance
- **Response Time** - < 500ms (p95)
- **Throughput** - 10,000 req/sec
- **Caching Strategy** - Multi-level caching
- **Database Optimization** - Proper indexing, connection pooling
- **CDN** - Static assets delivery

### ✅ Developer Experience
- **Clean Code** - SOLID principles
- **Testable** - Unit, integration, e2e tests
- **Well-documented** - Swagger/OpenAPI
- **CI/CD** - GitLab CI / GitHub Actions
- **Container-ready** - Docker + Kubernetes

---

## 🚀 Bắt Đầu

### Prerequisites
- .NET 8.0 SDK
- Docker Desktop
- Kubernetes (minikube/kind for local)
- Visual Studio 2022 / VS Code / Rider
- MySQL, MongoDB, Redis (or Docker containers)

### Local Development Setup
```bash
# Clone repository
git clone https://github.com/your-org/emis-system.git
cd emis-system

# Start infrastructure with Docker Compose
docker-compose up -d

# Restore dependencies
dotnet restore

# Run Identity Service
cd src/Services/Identity/Identity.API
dotnet run

# Run Student Service
cd src/Services/Student/Student.API
dotnet run

# ... (other services)
```

### Running with Docker Compose
```bash
# Build all services
docker-compose build

# Start all services
docker-compose up

# Access:
# - API Gateway: http://localhost:5000
# - Swagger UI: http://localhost:5000/swagger
```

### Running on Kubernetes
```bash
# Apply all manifests
kubectl apply -f k8s/

# Check status
kubectl get all -n emis-services

# Port forward API Gateway
kubectl port-forward svc/api-gateway 8080:80 -n emis-infrastructure

# Access: http://localhost:8080
```

---

## 📁 Project Structure

```
emis-clean-architecture-ddd/
├── docs/                           # Documentation
│   ├── 01-System-Overview.md
│   ├── 02-Microservices-Design.md
│   ├── 03-Domain-Models-and-Database.md
│   ├── 04-API-Contracts.md
│   ├── 05-Technology-Stack.md
│   └── 06-Deployment-Architecture.md
├── src/
│   ├── BuildingBlocks/             # Shared libraries
│   │   ├── EMIS.SharedKernel/      # Domain primitives
│   │   ├── EMIS.BuildingBlocks/    # Common utilities
│   │   └── EMIS.EventBus/          # Event bus abstraction
│   ├── Services/                   # Microservices
│   │   ├── Identity/
│   │   │   ├── Identity.API/
│   │   │   ├── Identity.Application/
│   │   │   ├── Identity.Domain/
│   │   │   └── Identity.Infrastructure/
│   │   ├── Student/
│   │   │   ├── Student.API/
│   │   │   ├── Student.Application/
│   │   │   ├── Student.Domain/
│   │   │   └── Student.Infrastructure/
│   │   ├── Teacher/
│   │   ├── Attendance/
│   │   ├── Assessment/
│   │   ├── NewsFeed/
│   │   ├── Chat/
│   │   ├── Payment/
│   │   ├── Menu/
│   │   ├── Leave/
│   │   ├── Camera/
│   │   ├── Report/
│   │   └── Notification/
│   └── ApiGateway/                 # YARP API Gateway
├── tests/
│   ├── UnitTests/
│   ├── IntegrationTests/
│   └── LoadTests/
├── k8s/                            # Kubernetes manifests
│   ├── infrastructure/
│   ├── services/
│   └── monitoring/
├── scripts/                        # Deployment scripts
├── docker-compose.yml
└── README.md
```

---

## 🧪 Testing

### Run Unit Tests
```bash
dotnet test tests/UnitTests/
```

### Run Integration Tests
```bash
dotnet test tests/IntegrationTests/
```

### Run Load Tests
```bash
k6 run tests/LoadTests/load-test.js
```

---

## 📊 Monitoring

### Prometheus Metrics
- Access: http://prometheus.emis.local:9090

### Grafana Dashboards
- Access: http://grafana.emis.local:3000
- Default credentials: admin / admin

### Kibana Logs
- Access: http://kibana.emis.local:5601

### Jaeger Tracing
- Access: http://jaeger.emis.local:16686

---

## 🔒 Security

### Authentication Flow
1. User logs in → Identity Service
2. Identity Service validates credentials
3. Returns JWT access token + refresh token
4. Client includes token in Authorization header
5. API Gateway validates token
6. Forwards request to appropriate service

### Multi-Tenant Isolation
- TenantId in JWT claims
- Global query filter in EF Core
- Separate database per tenant (optional)
- Tenant-based routing in API Gateway

---

## 📈 Performance Tips

### Caching
- Use Redis for frequently accessed data
- Implement response caching for read-heavy endpoints
- Cache invalidation on data updates

### Database
- Use proper indexes
- Implement query optimization
- Use read replicas for read operations
- Connection pooling

### API
- Implement pagination
- Use compression (Gzip/Brotli)
- Minimize payload size
- Use ETags for conditional requests

---

## 🤝 Contributing

### Development Workflow
1. Fork the repository
2. Create feature branch (`git checkout -b feature/amazing-feature`)
3. Commit changes (`git commit -m 'Add amazing feature'`)
4. Push to branch (`git push origin feature/amazing-feature`)
5. Open Pull Request

### Coding Standards
- Follow C# coding conventions
- Write unit tests for new features
- Update documentation
- Use meaningful commit messages

---

## 📞 Support

### Documentation
- [System Overview](./docs/01-System-Overview.md)
- [API Documentation](http://api.emis.local/swagger)
- [Architecture Decision Records](./docs/adr/)

### Contact
- Email: support@emis.com
- Slack: emis-team.slack.com
- Issue Tracker: GitHub Issues

---

## 📝 License

This project is proprietary software. All rights reserved.

Copyright © 2025 EMIS Team

---

## 🙏 Acknowledgments

- Clean Architecture - Robert C. Martin
- Domain-Driven Design - Eric Evans
- Microservices Patterns - Chris Richardson
- .NET Microservices Architecture - Microsoft

---

## 📅 Changelog

### Version 1.0.0 (2025-11-05)
- ✅ Initial architecture design
- ✅ Complete documentation
- ✅ 13 microservices specification
- ✅ Database schema design
- ✅ API contracts definition
- ✅ Kubernetes deployment manifests
- ✅ Technology stack selection

### Upcoming (Phase 1 - MVP)
- [ ] Implement Identity Service
- [ ] Implement Student Service
- [ ] Implement Teacher Service
- [ ] Implement Attendance Service
- [ ] Implement News Feed Service
- [ ] Implement Notification Service
- [ ] Mobile app (basic features)
- [ ] Web admin (basic features)

---

**Built with ❤️ by EMIS Team**
# emis-clean-architecture-ddd
# emis-clean-architecture-ddd

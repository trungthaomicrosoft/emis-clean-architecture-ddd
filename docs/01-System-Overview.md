# Hệ Thống Quản Lý Trường Mầm Non - EMIS (Educational Management Information System)

## 📋 Tổng Quan Dự Án

### Mục tiêu
Xây dựng hệ thống quản lý trường mầm non đa chức năng, kết nối giữa nhà trường và phụ huynh, hỗ trợ multi-tenant cho đến 25,000 trường học.

### Quy mô
- **Số lượng trường:** Lên đến 25,000 trường (multi-tenant)
- **Mỗi trường:**
  - ~30 giáo viên
  - ~500 học sinh
  - ~1,000 phụ huynh (mỗi học sinh có ít nhất 2 phụ huynh)
- **Tổng ước tính người dùng:** ~38,250,000 users (25K trường x 1,530 users/trường)

### Người dùng & Vai trò
- **Admin Hệ thống:** Quản lý toàn bộ hệ thống, onboard trường mới
- **Admin Trường:** Quản lý một trường cụ thể
- **Giáo viên:** Quản lý lớp học, học sinh, đăng bảng tin, nhận xét
- **Phụ huynh:** Xem thông tin con, nhận thông báo, chat với giáo viên
- **Quản lý Nhân sự:** Quản lý hồ sơ nhân viên
- **Kế toán:** Quản lý học phí, thanh toán

## 🎯 Chức Năng Chính

### 1. Quản Lý Học Sinh
- ✅ Hồ sơ học sinh (họ tên, giới tính, ngày sinh, mã học sinh, dân tộc, địa chỉ)
- ✅ Trạng thái học tập (đang học, đã nghỉ, bảo lưu, học thử)
- ✅ Quản lý phụ huynh
- ✅ Điểm danh hàng ngày
- ✅ Nhận xét hàng ngày
- ✅ Đánh giá theo tiêu chí (kèm hình ảnh, video)
- ✅ Theo dõi sự phát triển

### 2. Quản Lý Giáo Viên
- ✅ Hồ sơ giáo viên (họ tên, ngày sinh, giới tính, địa chỉ, số điện thoại)
- ✅ Quản lý vai trò
- ✅ Phân công lớp học
- ✅ Lịch dạy

### 3. Bảng Tin (News Feed)
- ✅ Đăng bài theo lớp (giáo viên chủ nhiệm)
- ✅ Đăng bài toàn trường (admin)
- ✅ Đính kèm hình ảnh, video
- ✅ Thông báo khẩn cấp

### 4. Hệ Thống Chat Real-time
- ✅ Chat 1-1 (phụ huynh ↔ giáo viên)
- ✅ Chat nhóm theo học sinh (phụ huynh + giáo viên + admin)
- ✅ Chat nhóm lớp (tất cả phụ huynh + giáo viên lớp)
- ✅ Chat nhóm riêng (custom group)
- ✅ Lưu trữ lịch sử chat lâu dài
- ✅ Đính kèm ảnh và file

### 5. Quản Lý Học Phí
- ✅ Tính toán học phí
- ✅ Thanh toán online
- ✅ Lịch sử giao dịch
- ✅ Nhắc nợ tự động

### 6. Quản Lý Thực Đơn
- ✅ Thực đơn hàng ngày/tuần
- ✅ Thông tin dinh dưỡng
- ✅ Thông báo cho phụ huynh

### 7. Quản Lý Nghỉ Phép
- ✅ Xin nghỉ (học sinh, giáo viên)
- ✅ Phê duyệt nghỉ phép
- ✅ Lịch sử nghỉ phép

### 8. Báo Cáo & Thống Kê
- ✅ Báo cáo điểm danh
- ✅ Báo cáo học phí
- ✅ Báo cáo sự phát triển học sinh
- ✅ Báo cáo nhân sự
- ✅ Dashboard quản lý

### 9. Camera Giám Sát
- ✅ Streaming trực tiếp
- ✅ Xem lại recording
- ✅ Phân quyền xem camera

## 🏗️ Kiến Trúc Tổng Quan

### Kiến Trúc Microservices + Clean Architecture + DDD

```
┌─────────────────────────────────────────────────────────────────┐
│                        API GATEWAY (YARP)                        │
│                  + Authentication & Authorization                │
└───────────────────────┬─────────────────────────────────────────┘
                        │
        ┌───────────────┼───────────────┐
        │               │               │
        ▼               ▼               ▼
┌──────────────┐ ┌──────────────┐ ┌──────────────┐
│   Identity   │ │   Student    │ │   Teacher    │
│   Service    │ │   Service    │ │   Service    │
└──────────────┘ └──────────────┘ └──────────────┘
        
        ▼               ▼               ▼
┌──────────────┐ ┌──────────────┐ ┌──────────────┐
│ Attendance   │ │   News Feed  │ │     Chat     │
│   Service    │ │   Service    │ │   Service    │
└──────────────┘ └──────────────┘ └──────────────┘

        ▼               ▼               ▼
┌──────────────┐ ┌──────────────┐ ┌──────────────┐
│   Payment    │ │     Menu     │ │    Leave     │
│   Service    │ │   Service    │ │   Service    │
└──────────────┘ └──────────────┘ └──────────────┘

        ▼               ▼               ▼
┌──────────────┐ ┌──────────────┐ ┌──────────────┐
│   Camera     │ │   Report     │ │ Notification │
│   Service    │ │   Service    │ │   Service    │
└──────────────┘ └──────────────┘ └──────────────┘
```

### Message Bus (Event-Driven Architecture)
```
┌─────────────────────────────────────────────────────────────────┐
│            RabbitMQ / Apache Kafka (Event Bus)                   │
│  - Domain Events                                                 │
│  - Integration Events                                            │
│  - Asynchronous Communication between Services                   │
└─────────────────────────────────────────────────────────────────┘
```

### Data Storage Strategy
- **MySQL:** Transactional data (Student, Teacher, Attendance, Payment...)
- **MongoDB:** Chat history, Logs, Documents
- **Redis:** Cache, Session, Real-time data
- **MinIO/S3:** File storage (Images, Videos, Documents)

### Real-time Communication
- **SignalR:** Real-time notifications, Chat
- **WebRTC:** Camera streaming

## 🎨 Clean Architecture Layers (Mỗi Microservice)

```
┌─────────────────────────────────────────────────────────┐
│                    API / Presentation                    │
│  - REST API Controllers                                  │
│  - gRPC Services                                         │
│  - SignalR Hubs                                          │
└────────────────────┬────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────┐
│                   Application Layer                      │
│  - Use Cases / Commands / Queries (CQRS)                │
│  - DTOs / View Models                                    │
│  - Application Services                                  │
│  - Validators (FluentValidation)                         │
└────────────────────┬────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────┐
│                    Domain Layer (DDD)                    │
│  - Entities / Aggregates                                 │
│  - Value Objects                                         │
│  - Domain Events                                         │
│  - Domain Services                                       │
│  - Repository Interfaces                                 │
└────────────────────┬────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────┐
│                 Infrastructure Layer                     │
│  - Repository Implementations                            │
│  - EF Core / Dapper                                      │
│  - External Services Integration                         │
│  - Message Bus Implementation                            │
└─────────────────────────────────────────────────────────┘
```

## 🔐 Multi-Tenancy Strategy

### Hybrid Approach: Database per Tenant (cho data isolation) + Shared Infrastructure

#### Tenant Identification
- **TenantId** trong mọi request (từ JWT token hoặc subdomain)
- Middleware để extract và validate TenantId
- Global Query Filter trong EF Core để auto-filter theo TenantId

#### Database Strategy
- **Shared Database + TenantId Column:** Cho metadata, configuration
- **Database per Tenant (Optional):** Cho trường có yêu cầu data isolation cao
- **Connection String Resolver:** Dynamic connection string dựa trên TenantId

#### Example:
```
tenant1.emis.com → TenantId: "tenant1" → Database: emis_tenant1
tenant2.emis.com → TenantId: "tenant2" → Database: emis_tenant2
```

## 🔒 Security & Authentication

### Identity Service (IdentityServer hoặc Keycloak)
- **Multi-tenant Authentication**
- **Role-based Access Control (RBAC)**
- **Permission-based Authorization**
- **JWT Token với Claims:**
  - TenantId
  - UserId
  - Roles
  - Permissions

### Security Features
- ✅ Password hashing (BCrypt/Argon2)
- ✅ JWT Token với refresh token
- ✅ Rate limiting
- ✅ API Key cho mobile app
- ✅ CORS configuration
- ✅ Data encryption at rest
- ✅ HTTPS/TLS

## 📱 Client Applications

### Mobile App (Flutter)
- **Phụ huynh:** Xem thông tin con, chat, nhận thông báo, xem camera
- **Giáo viên:** Điểm danh, nhận xét, đăng bảng tin, chat

### Web Application (Blazor Server / React / Angular)
- **Admin:** Quản lý toàn bộ hệ thống
- **Giáo viên:** Tất cả chức năng quản lý
- **Kế toán:** Quản lý học phí
- **HR:** Quản lý nhân sự

## 📊 Non-Functional Requirements

### Performance
- **Response Time:** < 500ms (p95)
- **Throughput:** 10,000 requests/second
- **Concurrent Users:** 100,000+

### Scalability
- **Horizontal Scaling:** Kubernetes auto-scaling
- **Database Sharding:** Khi cần thiết
- **CDN:** Cho static assets

### Availability
- **Uptime:** 99.9% (SLA)
- **Database Replication:** Master-Slave
- **Backup:** Daily automated backup

### Monitoring & Observability
- **Logging:** Serilog + ELK Stack (Elasticsearch, Logstash, Kibana)
- **Metrics:** Prometheus + Grafana
- **Tracing:** Jaeger / OpenTelemetry
- **Health Checks:** ASP.NET Core Health Checks

## 🚀 Deployment

### Kubernetes on-premise
- **Container Orchestration:** Kubernetes
- **Container Registry:** Harbor / Private Docker Registry
- **CI/CD:** GitLab CI / GitHub Actions
- **Infrastructure as Code:** Terraform / Helm Charts

### Environment
- **Development:** Local Docker Compose
- **Staging:** K8s Staging Cluster
- **Production:** K8s Production Cluster (HA setup)

## 📈 Phát Triển Theo Giai Đoạn

### Phase 1: MVP (3-4 months)
- ✅ Identity Service
- ✅ Student Service (cơ bản)
- ✅ Teacher Service (cơ bản)
- ✅ Attendance Service
- ✅ News Feed Service
- ✅ Notification Service
- ✅ Mobile App (basic features)
- ✅ Web Admin (basic features)

### Phase 2: Core Features (2-3 months)
- ✅ Chat Service (real-time)
- ✅ Payment Service
- ✅ Menu Service
- ✅ Leave Service
- ✅ Report Service (basic)

### Phase 3: Advanced Features (2-3 months)
- ✅ Camera Service
- ✅ Advanced Reporting & Analytics
- ✅ AI-powered features (Recommendations, Auto-grading)
- ✅ Performance optimization

### Phase 4: Scale & Optimize (Ongoing)
- ✅ Load testing & optimization
- ✅ Multi-region deployment
- ✅ Advanced monitoring
- ✅ Cost optimization

---

## 📚 Tài Liệu Liên Quan

- [02-Microservices-Design.md](./02-Microservices-Design.md) - Chi tiết từng microservice
- [03-Domain-Models.md](./03-Domain-Models.md) - Domain models & Database schema
- [04-API-Contracts.md](./04-API-Contracts.md) - API endpoints & contracts
- [05-Technology-Stack.md](./05-Technology-Stack.md) - Công nghệ & thư viện
- [06-Deployment-Architecture.md](./06-Deployment-Architecture.md) - Kiến trúc triển khai

---

**Version:** 1.0  
**Last Updated:** November 5, 2025  
**Author:** EMIS Architecture Team

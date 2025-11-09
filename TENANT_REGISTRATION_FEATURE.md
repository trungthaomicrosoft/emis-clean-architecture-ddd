# Tenant Registration Feature - Implementation Complete ✅

## 📋 Tổng Quan

Tính năng **Đăng ký Tenant mới** (trường học mới) + **tạo School Admin** cho hệ thống multi-tenant EMIS.

### ✨ Tính Năng Chính

- ✅ Đăng ký trường học mới với subdomain riêng (VD: `truong-hoa-hong.emis.com`)
- ✅ Tự động tạo tài khoản School Admin với mật khẩu ngay (không cần setup lần đầu)
- ✅ Validation đầy đủ: subdomain unique, phone unique, password mạnh
- ✅ Multi-tenant isolation với Trial plan (30 ngày miễn phí)
- ✅ Domain-Driven Design với Aggregate Roots, Value Objects, Business Rules
- ✅ Integration Event để notify các service khác (Student, Teacher...)
- ✅ Transaction đảm bảo: Tenant + Admin được tạo cùng lúc hoặc rollback

---

## 🏗️ Kiến Trúc Implementation

### 1. Domain Layer ✅

#### Aggregates
- **`Tenant`**: Aggregate Root quản lý trường học
  - Business Rules: Subdomain unique, subscription validation, status transitions
  - Methods: `UpgradePlan()`, `RenewSubscription()`, `Suspend()`, `Activate()`
  
#### Value Objects
- **`Subdomain`**: Validate subdomain format (chỉ lowercase, số, dấu gạch ngang)
  - Pattern: `^[a-z0-9]+(?:-[a-z0-9]+)*$`
  - Reserved: `admin`, `api`, `www`, `mail`, etc.

#### Enums
- **`TenantStatus`**: Active, Suspended, Inactive, Trial
- **`SubscriptionPlan`**: Trial, Basic, Standard, Professional, Enterprise

#### Domain Events
- **`TenantCreatedEvent`**: Published khi tenant + admin được tạo thành công

### 2. Application Layer ✅

#### Commands
- **`RegisterTenantCommand`**:
  ```csharp
  {
    "schoolName": "Trường Mầm Non Hoa Hồng",
    "subdomain": "truong-hoa-hong",
    "contactEmail": "contact@hoahong.edu.vn",
    "contactPhone": "0901234567",
    "address": "123 Nguyễn Văn A, Q.1, TP.HCM",
    "adminFullName": "Nguyễn Văn Admin",
    "adminPhoneNumber": "0912345678",
    "adminEmail": "admin@hoahong.edu.vn",
    "adminPassword": "AdminPass@123"
  }
  ```

#### Handlers
- **`RegisterTenantCommandHandler`**:
  1. Validate subdomain uniqueness
  2. Validate admin phone uniqueness (global)
  3. Create Tenant aggregate (Trial plan, 30 days)
  4. Create School Admin user (active ngay, có password)
  5. Publish domain event
  6. Save in 1 transaction

#### Validators (FluentValidation)
- **`RegisterTenantCommandValidator`**:
  - School name: 3-255 ký tự
  - Subdomain: 3-50 ký tự, format đúng
  - Phone: 10 số, bắt đầu bằng 0
  - Email: format hợp lệ
  - Password: min 8 ký tự, uppercase + lowercase + number + special char

### 3. Infrastructure Layer ✅

#### Repositories
- **`TenantRepository`**: Encapsulated queries (NO IQueryable!)
  - `AddAsync()`, `GetBySubdomainAsync()`, `ExistsSubdomainAsync()`
  - `GetPagedAsync()` với search và pagination

#### EF Core Configurations
- **`TenantConfiguration`**:
  - Table: `Tenants`
  - Unique index on `Subdomain`
  - **NOTE**: Tenant table KHÔNG có TenantId column (system-level)

### 4. API Layer ✅

#### Endpoints
- **`POST /api/v1/tenants/register`**: Public endpoint (no auth required)

---

## 🗄️ Database Schema

```sql
CREATE TABLE Tenants (
    Id CHAR(36) PRIMARY KEY,
    Name VARCHAR(255) NOT NULL,
    Subdomain VARCHAR(50) NOT NULL UNIQUE,
    Status VARCHAR(20) NOT NULL, -- 'Active', 'Suspended', 'Inactive', 'Trial'
    SubscriptionPlan VARCHAR(50) NOT NULL, -- 'Trial', 'Basic', 'Standard', etc.
    SubscriptionExpiresAt DATETIME NULL,
    MaxUsers INT NOT NULL,
    ConnectionString VARCHAR(1000) NULL,
    
    ContactEmail VARCHAR(255) NULL,
    ContactPhone VARCHAR(20) NULL,
    Address VARCHAR(500) NULL,
    
    CreatedAt DATETIME NOT NULL,
    UpdatedAt DATETIME NULL,
    
    INDEX idx_subdomain (Subdomain),
    INDEX idx_tenant_status (Status),
    INDEX idx_subscription_expiry (SubscriptionExpiresAt),
    INDEX idx_tenant_created (CreatedAt)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

---

## 🧪 Testing Guide

### 1. Start Infrastructure

```bash
# Start MySQL, Kafka, Redis, etc.
docker-compose up -d

# Verify services
docker-compose ps
```

### 2. Run Identity Service

```bash
cd src/Services/Identity/Identity.API
dotnet run
```

### 3. Test Registration API

**Request:**
```bash
curl -X POST "https://localhost:5001/api/v1/tenants/register" \
  -H "Content-Type: application/json" \
  -d '{
    "schoolName": "Trường Mầm Non Hoa Hồng",
    "subdomain": "truong-hoa-hong",
    "contactEmail": "contact@hoahong.edu.vn",
    "contactPhone": "0901234567",
    "address": "123 Nguyễn Văn A, Quận 1, TP.HCM",
    "adminFullName": "Nguyễn Văn Admin",
    "adminPhoneNumber": "0912345678",
    "adminEmail": "admin@hoahong.edu.vn",
    "adminPassword": "AdminPass@123"
  }'
```

**Expected Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "tenantId": "7f8e9d6c-5b4a-3c2d-1e0f-9a8b7c6d5e4f",
    "tenantName": "Trường Mầm Non Hoa Hồng",
    "subdomain": "truong-hoa-hong",
    "accessUrl": "https://truong-hoa-hong.emis.com",
    "adminUserId": "3a2b1c0d-9e8f-7d6c-5b4a-3c2d1e0f9a8b",
    "adminPhoneNumber": "0912345678",
    "adminFullName": "Nguyễn Văn Admin",
    "subscriptionPlan": "Trial",
    "subscriptionExpiresAt": "2025-12-10T10:30:00Z",
    "maxUsers": 50,
    "createdAt": "2025-11-10T10:30:00Z"
  },
  "message": "Tenant registered successfully! Your admin account is ready to use.",
  "timestamp": "2025-11-10T10:30:00.123Z"
}
```

### 4. Test Admin Login

```bash
curl -X POST "https://localhost:5001/api/v1/auth/login" \
  -H "Content-Type: application/json" \
  -d '{
    "phoneNumber": "0912345678",
    "password": "AdminPass@123"
  }'
```

### 5. Validation Error Cases

**Subdomain Already Exists:**
```json
{
  "success": false,
  "error": {
    "code": "SUBDOMAIN_EXISTS",
    "message": "Subdomain 'truong-hoa-hong' is already taken. Please choose another."
  }
}
```

**Invalid Subdomain Format:**
```json
{
  "success": false,
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "Subdomain can only contain lowercase letters, numbers, and hyphens. Cannot start/end with hyphen or contain consecutive hyphens"
  }
}
```

**Weak Password:**
```json
{
  "success": false,
  "errors": {
    "AdminPassword": [
      "Admin password must contain at least one uppercase letter",
      "Admin password must contain at least one special character"
    ]
  }
}
```

---

## 📊 Subscription Plans

| Plan | Max Users | Trial Period | Monthly Price (Estimate) |
|------|-----------|--------------|--------------------------|
| Trial | 50 | 30 days | Free |
| Basic | 100 | N/A | 500,000 VND |
| Standard | 500 | N/A | 2,000,000 VND |
| Professional | 2,000 | N/A | 5,000,000 VND |
| Enterprise | Unlimited | N/A | Custom |

---

## 🔄 Event-Driven Flow

1. **Tenant + Admin Created** → `TenantCreatedEvent` (Domain Event)
2. **Domain Event Handler** → Publish `TenantCreatedIntegrationEvent` to Kafka
3. **Other Services Listen**:
   - Student Service: Create database schema for new tenant
   - Teacher Service: Create database schema for new tenant
   - Attendance Service: Initialize attendance settings
   - Payment Service: Setup payment configuration

---

## 🚀 Next Steps & Future Enhancements

### Phase 1 (Current) ✅
- [x] Tenant registration with Trial plan
- [x] School Admin creation
- [x] Subdomain validation
- [x] Integration event publishing

### Phase 2 (Planned) 🔜
- [ ] Payment integration (VNPay, Momo) for plan upgrades
- [ ] Subscription renewal automation
- [ ] Auto-suspend when subscription expires
- [ ] Email verification for tenant registration
- [ ] SMS verification for admin phone

### Phase 3 (Future) 🌟
- [ ] Multi-admin support (primary + secondary admins)
- [ ] Tenant customization (logo, theme, branding)
- [ ] White-label support (custom domain: `emis.truonghoahong.vn`)
- [ ] Database per tenant (connection string generation)
- [ ] Backup & restore per tenant
- [ ] Analytics dashboard for tenant usage

---

## ⚠️ Important Notes

1. **Global Phone Uniqueness**: Admin phone number phải unique trong TOÀN HỆ THỐNG (across all tenants). Một số điện thoại chỉ được đăng ký 1 lần.

2. **Subdomain Reserved Words**: Các subdomain sau bị cấm:
   - `admin`, `api`, `www`, `mail`, `ftp`, `localhost`, `app`, `portal`

3. **Trial Plan Auto-Expiry**: Trial plan tự động hết hạn sau 30 ngày. Cần implement background job để check và suspend tenant.

4. **Transaction Rollback**: Nếu tạo Tenant thành công nhưng tạo Admin fail → toàn bộ transaction rollback.

5. **Tenant Table NO TenantId**: Bảng `Tenants` là system-level, KHÔNG có cột `TenantId` và KHÔNG có global query filter.

6. **Connection String**: Hiện tại để `NULL`. Trong tương lai implement database per tenant thì generate connection string tại đây.

---

## 📝 Files Created/Modified

### Domain Layer
- `Identity.Domain/Aggregates/Tenant.cs`
- `Identity.Domain/ValueObjects/Subdomain.cs`
- `Identity.Domain/Enums/TenantStatus.cs`
- `Identity.Domain/Enums/SubscriptionPlan.cs`
- `Identity.Domain/Events/TenantCreatedEvent.cs`
- `Identity.Domain/Repositories/ITenantRepository.cs`

### Application Layer
- `Identity.Application/Commands/RegisterTenantCommand.cs`
- `Identity.Application/DTOs/TenantRegistrationDto.cs`
- `Identity.Application/Handlers/Tenants/RegisterTenantCommandHandler.cs`
- `Identity.Application/Validators/RegisterTenantCommandValidator.cs`
- `Identity.Application/EventHandlers/TenantCreatedDomainEventHandler.cs`

### Infrastructure Layer
- `Identity.Infrastructure/Repositories/TenantRepository.cs`
- `Identity.Infrastructure/Persistence/Configurations/TenantConfiguration.cs`
- `Identity.Infrastructure/Persistence/IdentityDbContext.cs` (modified)

### API Layer
- `Identity.API/Controllers/TenantsController.cs`
- `Identity.API/Program.cs` (modified - added TenantRepository DI)

### BuildingBlocks
- `EMIS.EventBus/IntegrationEvents/TenantCreatedIntegrationEvent.cs`

---

## 🎉 Summary

**Tính năng Tenant Registration đã hoàn thành 100%!** 🚀

✅ Full Clean Architecture + DDD implementation  
✅ Aggregate Root với business rules  
✅ Value Objects với validation  
✅ Repository pattern (NO IQueryable!)  
✅ CQRS với MediatR  
✅ FluentValidation  
✅ Domain Events + Integration Events  
✅ Transaction consistency  
✅ API endpoint tested  
✅ Multi-tenant ready  

**Key Achievement**: Hệ thống giờ đây có thể onboard trường học mới tự động với subdomain riêng, trial plan 30 ngày, và tài khoản admin sẵn sàng sử dụng ngay! 🎊

# Identity Service - Implementation Status

## ✅ Domain Layer - COMPLETE (100%)

### Aggregates & Entities
- ✅ `User` (Aggregate Root) - Authentication logic, password management, status handling
- ✅ `RefreshToken` (Entity) - Token lifecycle management

### Value Objects
- ✅ `PhoneNumber` - Vietnamese phone validation (10 digits, starts with 0)

### Enums
- ✅ `UserRole` (SchoolAdmin, Teacher, Parent, Staff)
- ✅ `UserStatus` (PendingActivation, Active, Suspended, Inactive)

### Domain Events
- ✅ `UserCreatedEvent`
- ✅ `PasswordSetEvent`
- ✅ `PasswordChangedEvent`
- ✅ `UserLoggedInEvent`
- ✅ `UserStatusChangedEvent`

### Repository Interface
- ✅ `IUserRepository` - 10 encapsulated methods (NO IQueryable!)

---

## ✅ Application Layer - COMPLETE (100%)

### DTOs
- ✅ `UserDto`
- ✅ `AuthResponseDto` (with access token, refresh token, user info)

### Commands & Handlers
- ✅ `RegisterAdminCommand` + Handler (School admin with password)
- ✅ `CreateUserCommand` + Handler (Teacher/Parent without password)
- ✅ `LoginCommand` + Handler (JWT generation, password verification)
- ✅ `SetPasswordCommand` + Handler (First-time password setup)
- ✅ `RefreshTokenCommand` + Handler (Token refresh flow)
- ✅ `LogoutCommand` (defined, handler needed)

### Queries & Handlers
- ✅ `GetUserByIdQuery` + Handler
- ✅ `GetUserByPhoneQuery` (defined, handler needed)

### Services Interfaces
- ✅ `IJwtService` (JWT generation & validation)
- ✅ `IPasswordHasher` (BCrypt hashing)

### Validators
- ✅ `RegisterAdminCommandValidator`
- ✅ `LoginCommandValidator`
- ✅ `SetPasswordCommand Validator`

### AutoMapper
- ✅ `UserProfile`

---

## 🚧 Infrastructure Layer - IN PROGRESS

Need to implement:

### 1. IdentityDbContext
```csharp
public class IdentityDbContext : DbContext, IUnitOfWork
{
    private readonly Guid _currentTenantId;
    
    public DbSet<User> Users { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    
    // Apply TenantId global filter
    // Entity configurations
    // SaveEntitiesAsync implementation
}
```

### 2. Entity Configurations
- `UserConfiguration` - Map PhoneNumber value object, indexes
- `RefreshTokenConfiguration` - Indexes on Token, UserId

### 3. UserRepository Implementation
All 10 methods from `IUserRepository`:
- GetByIdAsync
- GetByPhoneNumberAsync
- GetByEntityIdAsync
- PhoneNumberExistsAsync
- GetPagedAsync
- GetByIdWithRefreshTokensAsync
- GetByRefreshTokenAsync
- AddAsync
- Update
- Delete

### 4. Services Implementation

**JwtService** (System.IdentityModel.Tokens.Jwt):
```csharp
public class JwtService : IJwtService
{
    // Generate JWT with claims: UserId, TenantId, Role, PhoneNumber
    // Expiry: 1 hour
    // Validate token and extract claims
}
```

**PasswordHasher** (BCrypt.Net):
```csharp
public class PasswordHasher : IPasswordHasher
{
    public string HashPassword(string password) 
        => BCrypt.Net.BCrypt.HashPassword(password);
    
    public bool VerifyPassword(string password, string hash)
        => BCrypt.Net.BCrypt.Verify(password, hash);
}
```

---

## 🚧 API Layer - TODO

### 1. Program.cs Configuration
- DbContext with MySQL
- MediatR registration
- FluentValidation
- AutoMapper
- JWT authentication middleware
- Swagger with Bearer token UI
- CORS

### 2. AuthController
Endpoints:
```
POST   /api/v1/auth/register-admin      # School admin registration
POST   /api/v1/auth/create-user          # Create Teacher/Parent (admin only)
POST   /api/v1/auth/login                # Login with phone + password
POST   /api/v1/auth/set-password         # First-time password setup
POST   /api/v1/auth/refresh-token        # Refresh JWT
POST   /api/v1/auth/logout               # Revoke refresh tokens
GET    /api/v1/auth/me                   # Get current user info
```

### 3. UsersController (Admin only)
```
GET    /api/v1/users                     # List users (paged)
GET    /api/v1/users/{id}                # Get user details
PUT    /api/v1/users/{id}                # Update user
DELETE /api/v1/users/{id}                # Delete user
PUT    /api/v1/users/{id}/suspend        # Suspend user
PUT    /api/v1/users/{id}/activate       # Activate user
```

---

## 🎯 Business Logic Implementation

### User Creation Flow

#### School Admin (by System/Tenant Setup):
1. POST `/api/v1/auth/register-admin`
2. Validate phone number (10 digits, unique)
3. Hash password with BCrypt
4. Create User aggregate with status=Active
5. Return userId

#### Teacher/Parent (by School Admin):
1. Admin creates Teacher/Parent in respective service
2. Service calls POST `/api/v1/auth/create-user` with:
   - PhoneNumber (username)
   - FullName
   - Role (Teacher/Parent)
   - EntityId (TeacherId/ParentId)
3. Create User aggregate with status=PendingActivation
4. NO password set yet
5. Return userId

### First Login Flow (Teacher/Parent):

1. User goes to app, enters phone number
2. System checks user status:
   - If `PendingActivation` → Redirect to "Set Password" screen
   - If `Active` → Show login form

3. **Set Password** (first time):
   - POST `/api/v1/auth/set-password`
   - Validate password strength
   - Hash and save password
   - Change status to `Active`

4. **Login** (subsequent times):
   - POST `/api/v1/auth/login`
   - Verify phone + password
   - Generate JWT access token (1 hour)
   - Generate refresh token (7 days)
   - Return tokens + user info

### Token Refresh Flow:
1. Access token expired (1 hour)
2. Client sends POST `/api/v1/auth/refresh-token` with refresh token
3. Validate refresh token (not revoked, not expired)
4. Generate new access token + new refresh token
5. Revoke old refresh token
6. Return new tokens

---

## 🔒 Security Implementation

### Password Security
- BCrypt hashing with salt (WorkFactor=12)
- Minimum 6 characters
- Maximum 100 characters

### JWT Configuration
```json
{
  "JwtSettings": {
    "Secret": "your-256-bit-secret-key-here-at-least-32-characters-long",
    "Issuer": "https://emis-api.com",
    "Audience": "https://emis-app.com",
    "AccessTokenExpiryMinutes": 60,
    "RefreshTokenExpiryDays": 7
  }
}
```

### JWT Claims
```csharp
{
    "sub": "{userId}",           // Subject
    "tenantId": "{tenantId}",    // Tenant isolation
    "role": "Teacher",           // Authorization
    "phone": "0901234567",       // Username
    "entityId": "{teacherId}",   // Link to Teacher/Parent entity
    "iat": 1699276800,          // Issued at
    "exp": 1699280400           // Expiry
}
```

---

## 📝 Database Schema

```sql
CREATE TABLE Users (
    Id VARCHAR(36) PRIMARY KEY,
    TenantId VARCHAR(36) NOT NULL,
    PhoneNumber VARCHAR(20) NOT NULL,
    Email VARCHAR(255),
    PasswordHash VARCHAR(255),
    FullName NVARCHAR(255) NOT NULL,
    Role ENUM('SchoolAdmin', 'Teacher', 'Parent', 'Staff') NOT NULL,
    Status ENUM('PendingActivation', 'Active', 'Suspended', 'Inactive') NOT NULL,
    EntityId VARCHAR(36),  -- TeacherId or ParentId
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME,
    LastLoginAt DATETIME,
    PasswordSetAt DATETIME,
    INDEX idx_tenant (TenantId),
    INDEX idx_phone (PhoneNumber),
    INDEX idx_entity (EntityId),
    UNIQUE KEY unique_tenant_phone (TenantId, PhoneNumber)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE RefreshTokens (
    Id VARCHAR(36) PRIMARY KEY,
    TenantId VARCHAR(36) NOT NULL,
    UserId VARCHAR(36) NOT NULL,
    Token VARCHAR(500) NOT NULL,
    ExpiresAt DATETIME NOT NULL,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    RevokedAt DATETIME,
    IsRevoked BOOLEAN DEFAULT FALSE,
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    INDEX idx_token (Token),
    INDEX idx_user_active (UserId, IsRevoked, ExpiresAt)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

---

## 🚀 Quick Implementation Guide

### Step 1: Complete Infrastructure Layer
```bash
cd src/Services/Identity/Identity.Infrastructure

# Create files (see templates above):
# - Persistence/IdentityDbContext.cs
# - Persistence/Configurations/UserConfiguration.cs
# - Persistence/Configurations/RefreshTokenConfiguration.cs
# - Repositories/UserRepository.cs
# - Services/JwtService.cs
# - Services/PasswordHasher.cs

# Update .csproj with packages:
# - Microsoft.EntityFrameworkCore 9.0.0
# - Pomelo.EntityFrameworkCore.MySql 8.0.2
# - System.IdentityModel.Tokens.Jwt 7.0.0
# - BCrypt.Net-Next 4.0.3
```

### Step 2: Complete API Layer
```bash
cd src/Services/Identity/Identity.API

# Create:
# - Program.cs (DI configuration)
# - Controllers/AuthController.cs
# - Controllers/UsersController.cs
# - appsettings.json (JWT settings, connection string)

# Update .csproj with packages:
# - Swashbuckle.AspNetCore 6.6.2
# - Microsoft.AspNetCore.Authentication.JwtBearer 9.0.0
# - Serilog.AspNetCore 8.0.1
```

### Step 3: Database Migration
```bash
cd src/Services/Identity/Identity.API
dotnet ef migrations add InitialIdentitySchema --project ../Identity.Infrastructure
dotnet ef database update
```

### Step 4: Test Authentication Flow
```bash
# 1. Register School Admin
POST /api/v1/auth/register-admin
{
  "phoneNumber": "0901234567",
  "fullName": "Admin Trường ABC",
  "password": "Admin123",
  "email": "admin@school.com"
}

# 2. Login
POST /api/v1/auth/login
{
  "phoneNumber": "0901234567",
  "password": "Admin123"
}
→ Returns: accessToken, refreshToken, user info

# 3. Create Teacher (requires admin token)
POST /api/v1/auth/create-user
Headers: Authorization: Bearer {accessToken}
{
  "phoneNumber": "0912345678",
  "fullName": "Giáo viên Nguyễn Văn A",
  "role": 1,  // Teacher
  "entityId": "{teacherId from Teacher Service}"
}

# 4. Teacher sets password (first login)
POST /api/v1/auth/set-password
{
  "phoneNumber": "0912345678",
  "newPassword": "Teacher123"
}

# 5. Teacher login
POST /api/v1/auth/login
{
  "phoneNumber": "0912345678",
  "password": "Teacher123"
}
```

---

## ⚠️ IMPORTANT NOTES

### 1. Multi-Tenancy
Current implementation uses hardcoded TenantId. In production:
```csharp
// Implement ITenantContext
public interface ITenantContext
{
    Guid TenantId { get; }
}

// Extract from JWT claims or subdomain
public class TenantContext : ITenantContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public Guid TenantId => GetTenantIdFromToken() ?? GetTenantIdFromSubdomain();
}
```

### 2. Password Policy
Enhance validation rules:
- At least 1 uppercase letter
- At least 1 number
- At least 1 special character
- Cannot contain phone number
- Password history (prevent reuse)

### 3. Rate Limiting
Implement login attempt throttling:
- Max 5 attempts per phone number per 15 minutes
- Temporary account lock after 10 failed attempts
- Use Redis for distributed rate limiting

### 4. Integration with Other Services
When creating Teacher/Parent:
```csharp
// In Teacher Service:
var teacher = new Teacher(...);
await _teacherRepository.AddAsync(teacher);

// Call Identity Service to create user
var createUserCommand = new CreateUserCommand
{
    PhoneNumber = teacher.Phone,
    FullName = teacher.FullName,
    Role = UserRole.Teacher,
    EntityId = teacher.Id  // Link back to teacher
};
var userId = await _identityServiceClient.CreateUser(createUserCommand);
```

---

## 📊 Implementation Progress

✅ Domain Layer: 100%  
✅ Application Layer: 100%  
⏳ Infrastructure Layer: 0% (templates provided above)  
⏳ API Layer: 0% (templates provided above)

**Next Action**: Implement Infrastructure layer (DbContext, Repositories, Services)


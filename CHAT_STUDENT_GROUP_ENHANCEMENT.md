# Chat Service - Student Group Creation Enhancement

## Vấn đề ban đầu

Client khi tạo Student Group Conversation chỉ có các thông tin cơ bản:
- `StudentId`
- `TenantId`
- `ClassId`
- `GroupName`

Nhưng `CreateStudentGroupCommand` yêu cầu client phải truyền thêm:
- `ParentIds[]` - Danh sách ID phụ huynh
- `TeacherIds[]` - Danh sách ID giáo viên

**Vấn đề**: Client không có và không nên biết thông tin này. Đây là business logic phía backend.

## Giải pháp

### 1. Đơn giản hóa Command

**File**: `Chat.Application/Commands/Conversations/CreateStudentGroupCommand.cs`

Xóa bỏ `ParentIds` và `TeacherIds`. Command giờ chỉ còn:
```csharp
public class CreateStudentGroupCommand : IRequest<ApiResponse<ConversationDto>>
{
    public Guid TenantId { get; set; }
    public Guid StudentId { get; set; }
    public Guid ClassId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public Guid CreatedBy { get; set; }
}
```

### 2. Tạo Integration Services

Backend tự động lấy thông tin từ các microservices khác.

#### Interface (Application Layer)

**File**: `Chat.Application/Interfaces/IStudentIntegrationService.cs`
```csharp
public interface IStudentIntegrationService
{
    Task<StudentInfoDto?> GetStudentWithParentsAsync(
        Guid tenantId, Guid studentId, CancellationToken ct);
}
```

**File**: `Chat.Application/Interfaces/ITeacherIntegrationService.cs`
```csharp
public interface ITeacherIntegrationService
{
    Task<List<TeacherInfoDto>> GetTeachersByClassIdAsync(
        Guid tenantId, Guid classId, CancellationToken ct);
}
```

#### Implementation (Infrastructure Layer)

**File**: `Chat.Infrastructure/Services/StudentIntegrationService.cs`
- Sử dụng `HttpClient` để gọi Student Service
- Endpoint: `GET /api/v1/students/{studentId}/with-parents?tenantId={tenantId}`

**File**: `Chat.Infrastructure/Services/TeacherIntegrationService.cs`
- Sử dụng `HttpClient` để gọi Teacher Service
- Endpoint: `GET /api/v1/teachers/by-class/{classId}?tenantId={tenantId}`

### 3. Cập nhật Handler Logic

**File**: `Chat.Application/Commands/Conversations/CreateStudentGroupCommandHandler.cs`

Flow mới:
1. ✅ Check student group đã tồn tại chưa
2. 🆕 **Gọi Student Service** - lấy thông tin học sinh + danh sách phụ huynh
3. 🆕 **Validate** - học sinh phải có ít nhất 1 phụ huynh
4. 🆕 **Gọi Teacher Service** - lấy danh sách giáo viên theo ClassId
5. 🆕 **Validate** - lớp phải có ít nhất 1 giáo viên
6. ✅ Tạo conversation với dữ liệu đã lấy được
7. ✅ Thêm các giáo viên phụ (nếu có nhiều hơn 1)
8. ✅ Lưu vào database

### 4. Dependency Injection Setup

**File**: `Chat.Infrastructure/DependencyInjection.cs`

Thêm method `AddIntegrationServices`:
```csharp
public static IServiceCollection AddIntegrationServices(
    this IServiceCollection services,
    IConfiguration configuration)
{
    // Student Service
    services.AddHttpClient<IStudentIntegrationService, StudentIntegrationService>(client =>
    {
        var baseUrl = configuration["IntegrationServices:StudentService:BaseUrl"] 
            ?? "http://localhost:5002";
        client.BaseAddress = new Uri(baseUrl);
        client.Timeout = TimeSpan.FromSeconds(30);
    });

    // Teacher Service
    services.AddHttpClient<ITeacherIntegrationService, TeacherIntegrationService>(client =>
    {
        var baseUrl = configuration["IntegrationServices:TeacherService:BaseUrl"] 
            ?? "http://localhost:5003";
        client.BaseAddress = new Uri(baseUrl);
        client.Timeout = TimeSpan.FromSeconds(30);
    });

    return services;
}
```

**File**: `Chat.API/Program.cs`
```csharp
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddIntegrationServices(builder.Configuration); // ← Thêm dòng này
```

### 5. Configuration

**File**: `Chat.API/appsettings.json` và `appsettings.Development.json`

```json
{
  "IntegrationServices": {
    "StudentService": {
      "BaseUrl": "http://localhost:5002"
    },
    "TeacherService": {
      "BaseUrl": "http://localhost:5003"
    }
  }
}
```

### 6. Package Dependencies

**Chat.Infrastructure.csproj**:
```xml
<PackageReference Include="Microsoft.Extensions.Http" Version="9.0.0" />
```

**Chat.Application.csproj**:
```xml
<PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="9.0.0" />
```

## Lợi ích của giải pháp

✅ **Separation of Concerns**: Client chỉ quan tâm đến business logic của mình (chọn học sinh)

✅ **Clean Architecture**: Application layer phụ thuộc vào interface, Infrastructure implement

✅ **Microservices Pattern**: Chat Service giao tiếp với Student/Teacher Service qua HTTP API

✅ **Error Handling**: Xử lý các trường hợp:
- Student không tồn tại
- Student không có phụ huynh
- Class không có giáo viên
- Service call timeout/failure

✅ **Logging**: Log chi tiết quá trình tạo group để dễ debug

✅ **Testability**: Có thể mock `IStudentIntegrationService` và `ITeacherIntegrationService` trong unit test

## Sử dụng API

### Request từ Client (Simplified)

```json
POST /api/v1/conversations/student-group
{
  "tenantId": "550e8400-e29b-41d4-a716-446655440000",
  "studentId": "660e8400-e29b-41d4-a716-446655440001",
  "classId": "770e8400-e29b-41d4-a716-446655440002",
  "groupName": "Group: Nguyễn Văn A",
  "createdBy": "880e8400-e29b-41d4-a716-446655440003"
}
```

### Backend tự động:
1. Fetch student info từ Student Service → nhận được `ParentIds[]` và student name
2. Fetch teachers từ Teacher Service → nhận được `TeacherIds[]`
3. Create conversation với tất cả thông tin đầy đủ

## Endpoints cần có ở các service khác

### Student Service
```
GET /api/v1/students/{studentId}/with-parents?tenantId={tenantId}
Response:
{
  "data": {
    "id": "...",
    "name": "Nguyễn Văn A",
    "classId": "...",
    "parents": [
      { "id": "...", "name": "Nguyễn Văn X", "relationship": "Father" },
      { "id": "...", "name": "Trần Thị Y", "relationship": "Mother" }
    ]
  },
  "success": true
}
```

### Teacher Service
```
GET /api/v1/teachers/by-class/{classId}?tenantId={tenantId}
Response:
{
  "data": [
    { "id": "...", "name": "Cô Hương", "isHeadTeacher": true },
    { "id": "...", "name": "Cô Lan", "isHeadTeacher": false }
  ],
  "success": true
}
```

## Testing

### Unit Test Mock Example
```csharp
var studentServiceMock = new Mock<IStudentIntegrationService>();
studentServiceMock
    .Setup(x => x.GetStudentWithParentsAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
    .ReturnsAsync(new StudentInfoDto 
    { 
        Id = studentId, 
        Name = "Test Student",
        Parents = new List<ParentInfoDto> 
        { 
            new() { Id = parentId, Name = "Test Parent" } 
        }
    });
```

## Notes

- ⚠️ **Resilience**: Nên thêm Polly để retry khi gọi HTTP failed
- ⚠️ **Caching**: Có thể cache danh sách teachers theo ClassId để giảm load
- ⚠️ **Timeout**: Đã set 30s timeout cho HTTP client, có thể điều chỉnh
- ⚠️ **Circuit Breaker**: Nên implement để tránh cascade failure giữa services

## Architecture Compliance

✅ **Clean Architecture**: Tuân thủ dependency rules (Application → Interface, Infrastructure → Implementation)

✅ **DDD**: Command đơn giản, business logic trong domain, orchestration trong handler

✅ **Microservices**: Communication qua HTTP, không share database

✅ **CQRS**: Command pattern với MediatR, trả về `ApiResponse<T>`

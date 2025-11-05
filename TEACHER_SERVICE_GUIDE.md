# Teacher Service Implementation Guide

## ✅ Đã Hoàn Thành

### 1. Domain Layer (100%)
- ✅ **Enums**: Gender, TeacherStatus, ClassAssignmentRole
- ✅ **Value Objects**: Address
- ✅ **Entities**: ClassAssignment
- ✅ **Aggregate Root**: Teacher với đầy đủ business rules:
  - Validation: phone number unique, age >= 18
  - Status management: Active, OnLeave, Resigned, Terminated
  - Class assignment logic
- ✅ **Domain Events**: 5 events
- ✅ **Repository Interface**: ITeacherRepository với encapsulated methods (NO IQueryable!)

### 2. Application Layer (Partial)
- ✅ **DTOs**: AddressDto, ClassAssignmentDto, TeacherDto, TeacherDetailDto
- ✅ **AutoMapper**: TeacherProfile
- ✅ **Commands**: CreateTeacherCommand, UpdateTeacherCommand, AssignTeacherToClassCommand
- ✅ **Queries**: GetTeachersQuery, GetTeacherByIdQuery
- ✅ **Project References**: Đã cấu hình MediatR, FluentValidation, AutoMapper

## 📋 Cần Hoàn Thành Tiếp

### 3. Application Layer - Handlers

#### CreateTeacherCommandHandler.cs
```csharp
using AutoMapper;
using EMIS.BuildingBlocks.ApiResponse;
using EMIS.BuildingBlocks.Exceptions;
using EMIS.SharedKernel;
using MediatR;
using Teacher.Application.Commands;
using Teacher.Application.DTOs;
using Teacher.Domain.Enums;
using Teacher.Domain.Repositories;
using Teacher.Domain.ValueObjects;

namespace Teacher.Application.Handlers;

public class CreateTeacherCommandHandler 
    : IRequestHandler<CreateTeacherCommand, ApiResponse<TeacherDetailDto>>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CreateTeacherCommandHandler(
        ITeacherRepository teacherRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _teacherRepository = teacherRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ApiResponse<TeacherDetailDto>> Handle(
        CreateTeacherCommand request,
        CancellationToken cancellationToken)
    {
        // Check phone number uniqueness
        if (await _teacherRepository.ExistsPhoneNumberAsync(request.PhoneNumber, null, cancellationToken))
        {
            return ApiResponse<TeacherDetailDto>.ErrorResult(
                $"Phone number {request.PhoneNumber} already exists",
                400);
        }

        // Get current tenant (from HttpContext or ClaimsPrincipal)
        // TODO: Implement ITenantContext to get current TenantId
        var tenantId = Guid.NewGuid(); // Placeholder

        // Create address value object
        Address? address = null;
        if (request.Address != null)
        {
            address = Address.Create(
                request.Address.Street,
                request.Address.Ward,
                request.Address.District,
                request.Address.City);
        }

        // Create teacher aggregate
        var teacher = new Domain.Aggregates.Teacher(
            tenantId,
            request.UserId,
            request.FullName,
            (Gender)request.Gender,
            request.PhoneNumber,
            request.HireDate);

        // Update additional info
        if (request.DateOfBirth.HasValue || !string.IsNullOrWhiteSpace(request.Email) || address != null)
        {
            teacher.UpdateInfo(
                request.FullName,
                (Gender)request.Gender,
                request.DateOfBirth,
                request.Email,
                address);
        }

        // Save to database
        await _teacherRepository.AddAsync(teacher, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        // Map to DTO
        var dto = _mapper.Map<TeacherDetailDto>(teacher);

        return ApiResponse<TeacherDetailDto>.SuccessResult(dto);
    }
}
```

#### GetTeachersQueryHandler.cs
```csharp
using AutoMapper;
using EMIS.BuildingBlocks.ApiResponse;
using EMIS.BuildingBlocks.Pagination;
using MediatR;
using Teacher.Application.DTOs;
using Teacher.Application.Queries;
using Teacher.Domain.Enums;
using Teacher.Domain.Repositories;

namespace Teacher.Application.Handlers;

public class GetTeachersQueryHandler 
    : IRequestHandler<GetTeachersQuery, ApiResponse<PagedResult<TeacherDto>>>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly IMapper _mapper;

    public GetTeachersQueryHandler(ITeacherRepository teacherRepository, IMapper mapper)
    {
        _teacherRepository = teacherRepository;
        _mapper = mapper;
    }

    public async Task<ApiResponse<PagedResult<TeacherDto>>> Handle(
        GetTeachersQuery request,
        CancellationToken cancellationToken)
    {
        // Convert status
        TeacherStatus? status = request.Status.HasValue 
            ? (TeacherStatus)request.Status.Value 
            : null;

        // Get paged data from repository
        var (items, totalCount) = await _teacherRepository.GetPagedAsync(
            request.PageNumber,
            request.PageSize,
            request.SearchTerm,
            status,
            cancellationToken);

        // Map to DTOs
        var dtos = _mapper.Map<List<TeacherDto>>(items);

        // Create paged result
        var pagedResult = new PagedResult<TeacherDto>(
            dtos,
            totalCount,
            request.PageNumber,
            request.PageSize);

        return ApiResponse<PagedResult<TeacherDto>>.SuccessResult(pagedResult);
    }
}
```

#### GetTeacherByIdQueryHandler.cs
```csharp
using AutoMapper;
using EMIS.BuildingBlocks.ApiResponse;
using MediatR;
using Teacher.Application.DTOs;
using Teacher.Application.Queries;
using Teacher.Domain.Repositories;

namespace Teacher.Application.Handlers;

public class GetTeacherByIdQueryHandler 
    : IRequestHandler<GetTeacherByIdQuery, ApiResponse<TeacherDetailDto>>
{
    private readonly ITeacherRepository _teacherRepository;
    private readonly IMapper _mapper;

    public GetTeacherByIdQueryHandler(ITeacherRepository teacherRepository, IMapper mapper)
    {
        _teacherRepository = teacherRepository;
        _mapper = mapper;
    }

    public async Task<ApiResponse<TeacherDetailDto>> Handle(
        GetTeacherByIdQuery request,
        CancellationToken cancellationToken)
    {
        // Get teacher with assignments
        var teacher = await _teacherRepository.GetByIdWithAssignmentsAsync(
            request.TeacherId,
            cancellationToken);

        if (teacher == null)
        {
            return ApiResponse<TeacherDetailDto>.ErrorResult(
                $"Teacher with id {request.TeacherId} not found",
                404);
        }

        // Map to DTO
        var dto = _mapper.Map<TeacherDetailDto>(teacher);

        return ApiResponse<TeacherDetailDto>.SuccessResult(dto);
    }
}
```

### 4. Application Layer - Validators

#### CreateTeacherCommandValidator.cs
```csharp
using FluentValidation;
using Teacher.Application.Commands;

namespace Teacher.Application.Validators;

public class CreateTeacherCommandValidator : AbstractValidator<CreateTeacherCommand>
{
    public CreateTeacherCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required")
            .MaximumLength(255).WithMessage("Full name cannot exceed 255 characters");

        RuleFor(x => x.Gender)
            .IsInEnum().WithMessage("Invalid gender value");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required")
            .Matches(@"^0\d{9}$").WithMessage("Phone number must be 10 digits starting with 0");

        RuleFor(x => x.Email)
            .EmailAddress().When(x => !string.IsNullOrEmpty(x.Email))
            .WithMessage("Invalid email format");

        RuleFor(x => x.DateOfBirth)
            .LessThan(DateTime.Now.AddYears(-18))
            .When(x => x.DateOfBirth.HasValue)
            .WithMessage("Teacher must be at least 18 years old");
    }
}
```

### 5. Infrastructure Layer - TeacherDbContext

```csharp
using Microsoft.EntityFrameworkCore;
using Teacher.Domain.Aggregates;
using Teacher.Domain.Entities;

namespace Teacher.Infrastructure.Persistence;

public class TeacherDbContext : DbContext
{
    private readonly Guid _currentTenantId; // TODO: Inject from ITenantContext

    public TeacherDbContext(DbContextOptions<TeacherDbContext> options)
        : base(options)
    {
        // TODO: Get from ITenantContext
        _currentTenantId = Guid.NewGuid();
    }

    public DbSet<Domain.Aggregates.Teacher> Teachers { get; set; } = null!;
    public DbSet<ClassAssignment> ClassAssignments { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TeacherDbContext).Assembly);

        // Global query filter for multi-tenancy
        modelBuilder.Entity<Domain.Aggregates.Teacher>()
            .HasQueryFilter(t => t.TenantId == _currentTenantId);

        modelBuilder.Entity<ClassAssignment>()
            .HasQueryFilter(ca => ca.TenantId == _currentTenantId);
    }
}
```

### 6. Infrastructure Layer - Entity Configurations

#### TeacherConfiguration.cs
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Teacher.Infrastructure.Persistence.Configurations;

public class TeacherConfiguration : IEntityTypeConfiguration<Domain.Aggregates.Teacher>
{
    public void Configure(EntityTypeBuilder<Domain.Aggregates.Teacher> builder)
    {
        builder.ToTable("Teachers");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.FullName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(t => t.PhoneNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(t => t.Email)
            .HasMaxLength(255);

        builder.Property(t => t.Avatar)
            .HasMaxLength(1000);

        builder.Property(t => t.Gender)
            .HasConversion<int>();

        builder.Property(t => t.Status)
            .HasConversion<int>();

        // Address value object
        builder.OwnsOne(t => t.Address, address =>
        {
            address.Property(a => a.Street).HasColumnName("Street").HasMaxLength(255);
            address.Property(a => a.Ward).HasColumnName("Ward").HasMaxLength(100);
            address.Property(a => a.District).HasColumnName("District").HasMaxLength(100);
            address.Property(a => a.City).HasColumnName("City").HasMaxLength(100);
        });

        // Class assignments
        builder.HasMany(t => t.ClassAssignments)
            .WithOne()
            .HasForeignKey(ca => ca.TeacherId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(t => t.TenantId);
        builder.HasIndex(t => t.UserId);
        builder.HasIndex(t => new { t.TenantId, t.PhoneNumber }).IsUnique();
        builder.HasIndex(t => t.Status);

        // Ignore domain events
        builder.Ignore(t => t.DomainEvents);
    }
}
```

### 7. Infrastructure Layer - TeacherRepository Implementation

Tạo file `/Infrastructure/Repositories/TeacherRepository.cs` implement tất cả methods trong `ITeacherRepository`. 

**Lưu ý quan trọng**: KHÔNG expose IQueryable, chỉ trả về kết quả cụ thể!

### 8. API Layer - TeachersController

```csharp
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Teacher.Application.Commands;
using Teacher.Application.Queries;

namespace Teacher.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class TeachersController : ControllerBase
{
    private readonly IMediator _mediator;

    public TeachersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetTeachers([FromQuery] GetTeachersQuery query)
    {
        var result = await _mediator.Send(query);
        return result.Success ? Ok(result) : StatusCode(result.StatusCode, result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTeacherById(Guid id)
    {
        var query = new GetTeacherByIdQuery(id);
        var result = await _mediator.Send(query);
        return result.Success ? Ok(result) : StatusCode(result.StatusCode, result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateTeacher([FromBody] CreateTeacherCommand command)
    {
        var result = await _mediator.Send(command);
        return result.Success ? Ok(result) : StatusCode(result.StatusCode, result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTeacher(Guid id, [FromBody] UpdateTeacherCommand command)
    {
        command.TeacherId = id;
        var result = await _mediator.Send(command);
        return result.Success ? Ok(result) : StatusCode(result.StatusCode, result);
    }

    [HttpPost("{id}/assign-class")]
    public async Task<IActionResult> AssignToClass(Guid id, [FromBody] AssignTeacherToClassCommand command)
    {
        command.TeacherId = id;
        var result = await _mediator.Send(command);
        return result.Success ? Ok(result) : StatusCode(result.StatusCode, result);
    }
}
```

## 🚀 Các Bước Tiếp Theo

1. **Tạo các Handlers còn lại** (UpdateTeacherCommandHandler, AssignTeacherToClassCommandHandler, etc.)
2. **Implement TeacherRepository** trong Infrastructure với tất cả methods
3. **Tạo EF Core Configurations** cho các entities
4. **Setup Dependency Injection** trong Program.cs
5. **Create và Apply Migration**: `dotnet ef migrations add InitialTeacherSchema`
6. **Test APIs** với Swagger UI

## 📚 Tham Khảo

- Student Service implementation tại `/src/Services/Student/`
- `docs/DDD-Repository-Pattern-Best-Practices.md`
- `.github/copilot-instructions.md`

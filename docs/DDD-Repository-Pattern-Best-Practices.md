# DDD Repository Pattern - Best Practices

## ❌ Vấn đề: IQueryable Trong Repository Vi Phạm DDD

### 1. **Leaky Abstraction**
```csharp
// ❌ BAD: Expose IQueryable - Application layer biết quá nhiều về persistence
public interface IStudentRepository
{
    IQueryable<Student> Query(); // Leak EF Core implementation details
}

// Application layer có thể viết bất kỳ query nào
var students = _repository.Query()
    .Include(s => s.Parents)           // EF Core specific
    .Where(s => s.Age > 5)             // Business logic leak
    .OrderBy(s => s.FullName)
    .ToListAsync();
```

**Vấn đề:**
- ❌ Application layer phụ thuộc vào EF Core
- ❌ Khó thay đổi ORM (VD: chuyển sang Dapper, MongoDB)
- ❌ Business logic bị phân tán (ở cả Handler và Repository)
- ❌ Khó test và mock
- ❌ Vi phạm Dependency Inversion Principle

### 2. **Vi Phạm Encapsulation**
```csharp
// ❌ BAD: Repository không kiểm soát được cách data được query
public class GetStudentsHandler
{
    public async Task<List<StudentDto>> Handle(GetStudentsQuery request)
    {
        // Business rules scattered in application layer
        var query = _repository.Query()
            .Where(s => s.Status == StudentStatus.Active)  // Business rule #1
            .Where(s => s.Parents.Count >= 1);              // Business rule #2
            
        // What if other handlers query differently?
        // No consistency guarantee!
    }
}
```

### 3. **Testability Issues**
```csharp
// ❌ HARD TO TEST: Cannot easily mock IQueryable behavior
[Fact]
public void Should_Return_Active_Students()
{
    var mockRepo = new Mock<IStudentRepository>();
    // How to mock Query()? Very complex!
    mockRepo.Setup(r => r.Query()).Returns(???); // Difficult
}
```

---

## ✅ Giải Pháp: Encapsulated Repository Methods (DDD Compliant)

### 1. **Định nghĩa Specific Methods trong Repository**

```csharp
// ✅ GOOD: Repository interface với methods cụ thể
public interface IStudentRepository : IRepository<Student>
{
    // Specific, intention-revealing methods
    Task<Student?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    
    Task<Student?> GetByCodeAsync(StudentCode code, CancellationToken cancellationToken = default);
    
    // Encapsulated pagination with filters
    Task<(IEnumerable<Student> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        StudentStatus? status = null,
        Guid? classId = null,
        CancellationToken cancellationToken = default);
    
    // Domain-specific query
    Task<(IEnumerable<Student> Items, int TotalCount)> GetStudentsWithParentsPagedAsync(
        int pageNumber,
        int pageSize,
        int minParentCount = 1,
        string? searchTerm = null,
        StudentStatus? status = null,
        Guid? classId = null,
        CancellationToken cancellationToken = default);
}
```

**Lợi ích:**
- ✅ **Clear Intent**: Method name nói rõ mục đích
- ✅ **Encapsulation**: Query logic ẩn trong Repository
- ✅ **Testability**: Dễ mock với input/output cụ thể
- ✅ **Reusability**: Nhiều handler có thể dùng chung method
- ✅ **Maintainability**: Thay đổi query logic chỉ ở 1 chỗ

### 2. **Implementation trong Infrastructure Layer**

```csharp
// ✅ GOOD: All query logic encapsulated in Repository
public class StudentRepository : IStudentRepository
{
    private readonly StudentDbContext _context;

    public async Task<(IEnumerable<Student> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm,
        StudentStatus? status,
        Guid? classId,
        CancellationToken cancellationToken)
    {
        // IQueryable used INTERNALLY - not exposed
        var query = _context.Students.AsQueryable();

        // All filtering logic encapsulated here
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var searchLower = searchTerm.ToLower();
            query = query.Where(s =>
                s.FullName.ToLower().Contains(searchLower) ||
                s.StudentCode.ToString().ToLower().Contains(searchLower));
        }

        if (status.HasValue)
            query = query.Where(s => s.Status == status.Value);

        if (classId.HasValue)
            query = query.Where(s => s.ClassId == classId.Value);

        // Execute query
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(s => s.FullName)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
```

### 3. **Clean Handler trong Application Layer**

```csharp
// ✅ GOOD: Handler chỉ lo orchestration, không lo query details
public class GetStudentsQueryHandler : IRequestHandler<GetStudentsQuery, ApiResponse<PagedResult<StudentDto>>>
{
    private readonly IStudentRepository _studentRepository;
    private readonly IMapper _mapper;

    public async Task<ApiResponse<PagedResult<StudentDto>>> Handle(
        GetStudentsQuery request, 
        CancellationToken cancellationToken)
    {
        // Simple call to repository method
        var (items, totalCount) = await _studentRepository.GetPagedAsync(
            request.PageNumber,
            request.PageSize,
            request.SearchTerm,
            request.Status.HasValue ? (StudentStatus)request.Status.Value : null,
            request.ClassId,
            cancellationToken);

        // Focus on business logic: mapping, response creation
        var dtos = _mapper.Map<List<StudentDto>>(items);
        var pagedResult = new PagedResult<StudentDto>(dtos, totalCount, request.PageNumber, request.PageSize);
        
        return ApiResponse<PagedResult<StudentDto>>.SuccessResult(pagedResult);
    }
}
```

---

## 📊 So Sánh: Before vs After

| Aspect | ❌ IQueryable Exposed | ✅ Encapsulated Methods |
|--------|----------------------|-------------------------|
| **DDD Compliance** | Vi phạm | Tuân thủ |
| **Encapsulation** | Thấp - Application layer biết DB structure | Cao - Query logic ẩn trong Repository |
| **Testability** | Khó mock IQueryable | Dễ mock với input/output cụ thể |
| **Maintainability** | Query logic phân tán | Query logic tập trung |
| **Reusability** | Mỗi handler tự viết query | Repository methods dùng chung |
| **ORM Independence** | Phụ thuộc EF Core | Có thể thay đổi ORM dễ dàng |
| **Performance** | ✅ Tốt (database-level) | ✅ Tốt (database-level) |
| **Code Duplication** | Cao (nhiều handler viết query giống nhau) | Thấp (reuse repository methods) |

---

## 🎯 DDD Principles Applied

### 1. **Separation of Concerns**
- **Domain Layer**: Định nghĩa contracts (interfaces)
- **Infrastructure Layer**: Implementation với EF Core
- **Application Layer**: Business orchestration, không biết về DB

### 2. **Dependency Inversion**
```
Application Layer (high-level) 
    ↓ depends on
Domain Layer (abstractions - IStudentRepository)
    ↑ implemented by
Infrastructure Layer (low-level - StudentRepository)
```

### 3. **Encapsulation**
- Repository ẩn toàn bộ query complexity
- Application layer chỉ gọi methods với parameters business-relevant

### 4. **Ubiquitous Language**
```csharp
// Methods reflect domain concepts
GetStudentsWithParentsPagedAsync(...)  // Clear business intent
GetByCodeAsync(StudentCode code)       // Domain-specific value object
```

---

## 🚀 Performance: Vẫn Tối Ưu!

Mặc dù không expose IQueryable, performance vẫn tốt vì:

```csharp
// IQueryable used INTERNALLY in Repository
var query = _context.Students.AsQueryable();
query = query.Where(...);  // Deferred execution
var count = await query.CountAsync();  // SQL: SELECT COUNT(*)
var items = await query.Skip(...).Take(...).ToListAsync();  // SQL: LIMIT/OFFSET
```

**SQL Generated:**
```sql
-- Count query
SELECT COUNT(*) FROM Students 
WHERE FullName LIKE @p0 AND Status = @p1;

-- Data query with pagination
SELECT * FROM Students 
WHERE FullName LIKE @p0 AND Status = @p1
ORDER BY FullName
LIMIT @p2 OFFSET @p3;
```

✅ **Database-level filtering**
✅ **No N+1 queries**
✅ **Optimal SQL generation**
✅ **Indexes utilized**

---

## 📝 Khi Nào Nên Dùng IQueryable?

### ✅ OK to use IQueryable:
1. **INTERNAL trong Repository implementation** ✅
2. **Private methods trong Repository** ✅
3. **Infrastructure layer tests** ✅

### ❌ KHÔNG nên expose IQueryable:
1. **Repository interface** ❌
2. **Return type của public methods** ❌
3. **Cross layer boundaries** ❌

---

## 🎓 Specification Pattern (Alternative)

Nếu cần query phức tạp hơn, có thể dùng **Specification Pattern**:

```csharp
// Specification for complex business rules
public interface ISpecification<T>
{
    Expression<Func<T, bool>> Criteria { get; }
    List<Expression<Func<T, object>>> Includes { get; }
    Expression<Func<T, object>>? OrderBy { get; }
}

public interface IStudentRepository
{
    Task<List<Student>> FindAsync(ISpecification<Student> spec);
}

// Usage
var spec = new ActiveStudentsWithParentsSpec();
var students = await _repository.FindAsync(spec);
```

Nhưng với yêu cầu hiện tại, **Encapsulated Methods** đã đủ và đơn giản hơn!

---

## ✅ Kết Luận

**Refactoring này đạt được:**
1. ✅ **Tuân thủ DDD**: Repository encapsulates query logic
2. ✅ **Clean Architecture**: Proper layer separation
3. ✅ **Performance**: Database-level filtering maintained
4. ✅ **Testability**: Easy to mock repository methods
5. ✅ **Maintainability**: Query logic centralized
6. ✅ **Reusability**: Methods shared across handlers

**Trade-offs:**
- ➕ More repository methods (nhưng rõ ràng hơn)
- ➕ Phải define methods cho mỗi query pattern (nhưng kiểm soát tốt hơn)
- ➖ Ít flexible hơn IQueryable raw (nhưng đó là điều tốt trong DDD!)

**Recommendation:** ✅ Use **Encapsulated Repository Methods** for production DDD applications!

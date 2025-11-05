using EMIS.BuildingBlocks.ApiResponse;
using EMIS.BuildingBlocks.Pagination;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Student.Application.Commands.Students;
using Student.Application.DTOs;
using Student.Application.Queries.Students;

namespace Student.API.Controllers;

/// <summary>
/// Controller for Student management
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class StudentsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<StudentsController> _logger;

    public StudentsController(IMediator mediator, ILogger<StudentsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get paginated list of students
    /// </summary>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10)</param>
    /// <param name="searchTerm">Search term for filtering</param>
    /// <param name="classId">Filter by class ID</param>
    /// <param name="status">Filter by status</param>
    /// <returns>Paginated list of students</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<StudentDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStudents(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null,
        [FromQuery] Guid? classId = null,
        [FromQuery] int? status = null)
    {
        var query = new GetStudentsQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            SearchTerm = searchTerm,
            ClassId = classId,
            Status = status
        };

        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get student by ID
    /// </summary>
    /// <param name="id">Student ID</param>
    /// <returns>Student details with parents</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<StudentDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<StudentDetailDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStudentById(Guid id)
    {
        var query = new GetStudentByIdQuery(id);
        var result = await _mediator.Send(query);

        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    /// <summary>
    /// Get student by code
    /// </summary>
    /// <param name="code">Student code (e.g., HS2025000001)</param>
    /// <returns>Student details</returns>
    [HttpGet("by-code/{code}")]
    [ProducesResponseType(typeof(ApiResponse<StudentDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<StudentDetailDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStudentByCode(string code)
    {
        var query = new GetStudentByCodeQuery(code);
        var result = await _mediator.Send(query);

        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    /// <summary>
    /// Get students by class
    /// </summary>
    /// <param name="classId">Class ID</param>
    /// <returns>List of students in the class</returns>
    [HttpGet("by-class/{classId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<List<StudentDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStudentsByClass(Guid classId)
    {
        var query = new GetStudentsByClassQuery(classId);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Create a new student
    /// </summary>
    /// <param name="command">Student creation data</param>
    /// <returns>Created student details</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<StudentDetailDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<StudentDetailDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateStudent([FromBody] CreateStudentCommand command)
    {
        _logger.LogInformation("Creating new student: {FullName}", command.FullName);

        var result = await _mediator.Send(command);

        if (!result.Success)
            return BadRequest(result);

        return CreatedAtAction(
            nameof(GetStudentById),
            new { id = result.Data?.Id },
            result);
    }

    /// <summary>
    /// Update student information
    /// </summary>
    /// <param name="id">Student ID</param>
    /// <param name="command">Update data</param>
    /// <returns>Updated student details</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<StudentDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<StudentDetailDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<StudentDetailDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStudent(Guid id, [FromBody] UpdateStudentCommand command)
    {
        if (id != command.Id)
            return BadRequest(ApiResponse<StudentDetailDto>.ErrorResult("ID mismatch"));

        _logger.LogInformation("Updating student: {Id}", id);

        var result = await _mediator.Send(command);

        if (!result.Success)
            return result.Error?.Code == "NOT_FOUND" ? NotFound(result) : BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Delete a student
    /// </summary>
    /// <param name="id">Student ID</param>
    /// <returns>Success status</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteStudent(Guid id)
    {
        _logger.LogWarning("Deleting student: {Id}", id);

        var command = new DeleteStudentCommand(id);
        var result = await _mediator.Send(command);

        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    /// <summary>
    /// Change student status
    /// </summary>
    /// <param name="id">Student ID</param>
    /// <param name="status">New status (1=Active, 2=Inactive, 3=Graduated, 4=Transferred, 5=Expelled)</param>
    /// <returns>Success status</returns>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangeStatus(Guid id, [FromBody] int status)
    {
        _logger.LogInformation("Changing status for student {Id} to {Status}", id, status);

        var command = new ChangeStudentStatusCommand(id, status);
        var result = await _mediator.Send(command);

        if (!result.Success)
            return result.Error?.Code == "NOT_FOUND" ? NotFound(result) : BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Lấy danh sách học sinh đã có phụ huynh
    /// </summary>
    /// <param name="pageNumber">Số trang (mặc định: 1)</param>
    /// <param name="pageSize">Số lượng bản ghi trên trang (mặc định: 10)</param>
    /// <param name="searchTerm">Tìm kiếm theo tên hoặc mã học sinh</param>
    /// <param name="status">Lọc theo trạng thái học sinh</param>
    /// <param name="classId">Lọc theo lớp học</param>
    /// <param name="minParentCount">Số lượng phụ huynh tối thiểu (mặc định: 1)</param>
    /// <returns>Danh sách học sinh có phụ huynh với phân trang</returns>
    [HttpGet("with-parents")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<StudentDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStudentsWithParents(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null,
        [FromQuery] Domain.Enums.StudentStatus? status = null,
        [FromQuery] Guid? classId = null,
        [FromQuery] int minParentCount = 1)
    {
        _logger.LogInformation("Getting students with parents. PageNumber: {PageNumber}, MinParentCount: {MinParentCount}", 
            pageNumber, minParentCount);

        var query = new GetStudentsWithParentsQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            SearchTerm = searchTerm,
            Status = status,
            ClassId = classId,
            MinParentCount = minParentCount
        };

        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult HealthCheck()
    {
        return Ok(new { status = "healthy", service = "Student.API", timestamp = DateTime.UtcNow });
    }
}

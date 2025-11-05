using MediatR;
using Microsoft.AspNetCore.Mvc;
using Teacher.Application.Commands;
using Teacher.Application.Queries;
using Teacher.Domain.Enums;

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

    /// <summary>
    /// Lấy danh sách giáo viên có phân trang
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetTeachers(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null,
        [FromQuery] TeacherStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetTeachersQuery 
        { 
            PageNumber = pageNumber, 
            PageSize = pageSize, 
            SearchTerm = searchTerm, 
            Status = (int?)status 
        };
        var result = await _mediator.Send(query, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Lấy thông tin chi tiết giáo viên theo Id
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetTeacherById(Guid id)
    {
        var query = new GetTeacherByIdQuery(id);
        var result = await _mediator.Send(query);
        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>
    /// Tạo giáo viên mới
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateTeacher([FromBody] CreateTeacherCommand command)
    {
        var result = await _mediator.Send(command);
        return result.Success ? CreatedAtAction(nameof(GetTeacherById), new { id = result.Data?.Id }, result) : BadRequest(result);
    }

    /// <summary>
    /// Cập nhật thông tin giáo viên
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTeacher(Guid id, [FromBody] UpdateTeacherCommand command)
    {
        command.TeacherId = id;
        var result = await _mediator.Send(command);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Xóa giáo viên
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTeacher(Guid id)
    {
        var command = new DeleteTeacherCommand(id);
        var result = await _mediator.Send(command);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Phân công giáo viên vào lớp
    /// </summary>
    [HttpPost("{id}/assign-class")]
    public async Task<IActionResult> AssignToClass(Guid id, [FromBody] AssignTeacherToClassCommand command)
    {
        command.TeacherId = id;
        var result = await _mediator.Send(command);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Gỡ phân công giáo viên khỏi lớp
    /// </summary>
    [HttpPost("{id}/unassign-class")]
    public async Task<IActionResult> UnassignFromClass(Guid id, [FromBody] UnassignTeacherFromClassCommand command)
    {
        command.TeacherId = id;
        var result = await _mediator.Send(command);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}

using Identity.Application.Commands;
using Identity.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Identity.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Đăng ký School Admin (có password ngay)
    /// </summary>
    [HttpPost("register-admin")]
    public async Task<IActionResult> RegisterAdmin([FromBody] RegisterAdminCommand command)
    {
        var result = await _mediator.Send(command);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Tạo user mới (Teacher/Parent) - chưa có password
    /// Chỉ Admin mới được gọi endpoint này
    /// </summary>
    [HttpPost("create-user")]
    [Authorize(Roles = "SchoolAdmin")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserCommand command)
    {
        var result = await _mediator.Send(command);
        return result.Success ? CreatedAtAction(nameof(GetUserById), new { id = result.Data }, result) : BadRequest(result);
    }

    /// <summary>
    /// Đăng nhập (phone + password)
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginCommand command)
    {
        var result = await _mediator.Send(command);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Thiết lập mật khẩu lần đầu (Teacher/Parent)
    /// </summary>
    [HttpPost("set-password")]
    public async Task<IActionResult> SetPassword([FromBody] SetPasswordCommand command)
    {
        var result = await _mediator.Send(command);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Refresh access token
    /// </summary>
    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenCommand command)
    {
        var result = await _mediator.Send(command);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Đăng xuất (revoke refresh tokens)
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var userIdClaim = User.FindFirst("sub");
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized();

        var command = new LogoutCommand { UserId = userId };
        var result = await _mediator.Send(command);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Lấy thông tin user hiện tại
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetMe()
    {
        var userIdClaim = User.FindFirst("sub");
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized();

        var query = new GetUserByIdQuery(userId);
        var result = await _mediator.Send(query);
        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>
    /// Lấy thông tin user theo Id (for internal route)
    /// </summary>
    [HttpGet("users/{id}")]
    [Authorize]
    private async Task<IActionResult> GetUserById(Guid id)
    {
        var query = new GetUserByIdQuery(id);
        var result = await _mediator.Send(query);
        return result.Success ? Ok(result) : NotFound(result);
    }
}

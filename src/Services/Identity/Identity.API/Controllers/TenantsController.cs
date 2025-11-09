using Identity.Application.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Identity.API.Controllers;

/// <summary>
/// Tenant management controller
/// Handles tenant registration and management
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class TenantsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<TenantsController> _logger;

    public TenantsController(IMediator mediator, ILogger<TenantsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Đăng ký tenant mới (trường học mới) + tạo School Admin
    /// Public endpoint - không cần authentication
    /// </summary>
    /// <remarks>
    /// Sample request:
    /// 
    ///     POST /api/v1/tenants/register
    ///     {
    ///        "schoolName": "Trường Mầm Non Hoa Hồng",
    ///        "subdomain": "truong-hoa-hong",
    ///        "contactEmail": "contact@hoahong.edu.vn",
    ///        "contactPhone": "0901234567",
    ///        "address": "123 Nguyễn Văn A, Q.1, TP.HCM",
    ///        "adminFullName": "Nguyễn Văn Admin",
    ///        "adminPhoneNumber": "0912345678",
    ///        "adminEmail": "admin@hoahong.edu.vn",
    ///        "adminPassword": "AdminPass@123"
    ///     }
    /// 
    /// Response:
    /// - 200 OK: Tenant created successfully with admin account
    /// - 400 Bad Request: Validation errors or subdomain already exists
    /// - 500 Internal Server Error: Server error
    /// </remarks>
    /// <param name="command">Tenant registration information</param>
    /// <returns>Tenant registration result with access credentials</returns>
    [HttpPost("register")]
    [ProducesResponseType(typeof(EMIS.BuildingBlocks.ApiResponse.ApiResponse<Identity.Application.DTOs.TenantRegistrationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RegisterTenant([FromBody] RegisterTenantCommand command)
    {
        _logger.LogInformation("Registering new tenant: {SchoolName}, Subdomain: {Subdomain}", 
            command.SchoolName, command.Subdomain);

        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            _logger.LogWarning("Tenant registration failed: {ErrorCode} - {ErrorMessage}", 
                result.Error?.Code, result.Error?.Message);
            return BadRequest(result);
        }

        _logger.LogInformation("Tenant registered successfully: TenantId={TenantId}, AdminUserId={AdminUserId}", 
            result.Data?.TenantId, result.Data?.AdminUserId);

        return Ok(result);
    }

    // TODO: Add additional endpoints for tenant management
    // - GET /api/v1/tenants/{id} - Get tenant details (admin only)
    // - PUT /api/v1/tenants/{id} - Update tenant info (admin only)
    // - POST /api/v1/tenants/{id}/upgrade - Upgrade subscription plan
    // - POST /api/v1/tenants/{id}/suspend - Suspend tenant
}

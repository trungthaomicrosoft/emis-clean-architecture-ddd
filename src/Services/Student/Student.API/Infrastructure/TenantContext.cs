using EMIS.BuildingBlocks.MultiTenant;

namespace Student.API.Infrastructure;

/// <summary>
/// Mock implementation of ITenantContext for development
/// In production, this would be resolved from JWT token or request header
/// </summary>
public class MockTenantContext : ITenantContext
{
    public Guid TenantId => Guid.Parse("00000000-0000-0000-0000-000000000001"); // Mock tenant ID

    public string? TenantName => "Demo School";

    public bool IsAvailable => true;
}

/// <summary>
/// HTTP-based tenant context resolver
/// Resolves tenant from HTTP header or JWT token
/// </summary>
public class HttpTenantContext : ITenantContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpTenantContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid TenantId
    {
        get
        {
            // Try to get from header first
            var tenantIdHeader = _httpContextAccessor.HttpContext?.Request.Headers["X-Tenant-Id"].FirstOrDefault();
            if (!string.IsNullOrEmpty(tenantIdHeader) && Guid.TryParse(tenantIdHeader, out var tenantId))
            {
                return tenantId;
            }

            // TODO: In production, extract from JWT claims
            // var tenantIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("tenant_id")?.Value;
            
            // Fallback to default tenant for development
            return Guid.Parse("00000000-0000-0000-0000-000000000001");
        }
    }

    public string? TenantName
    {
        get
        {
            return _httpContextAccessor.HttpContext?.Request.Headers["X-Tenant-Name"].FirstOrDefault() 
                ?? "Unknown School";
        }
    }

    public bool IsAvailable => _httpContextAccessor.HttpContext != null;
}

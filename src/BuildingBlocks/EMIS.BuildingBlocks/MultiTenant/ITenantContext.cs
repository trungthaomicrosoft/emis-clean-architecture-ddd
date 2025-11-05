namespace EMIS.BuildingBlocks.MultiTenant;

/// <summary>
/// Interface for tenant context provider.
/// </summary>
public interface ITenantContext
{
    /// <summary>
    /// Gets the current tenant ID.
    /// </summary>
    Guid TenantId { get; }

    /// <summary>
    /// Gets the current tenant name.
    /// </summary>
    string? TenantName { get; }

    /// <summary>
    /// Checks if tenant context is available.
    /// </summary>
    bool IsAvailable { get; }
}

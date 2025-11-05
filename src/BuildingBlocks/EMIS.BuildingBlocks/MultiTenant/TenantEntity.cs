using EMIS.SharedKernel;

namespace EMIS.BuildingBlocks.MultiTenant;

/// <summary>
/// Base entity with tenant isolation support.
/// Inherits from Entity to get Id and domain event support.
/// </summary>
public abstract class TenantEntity : Entity
{
    public Guid TenantId { get; protected set; }

    protected TenantEntity()
    {
    }

    protected TenantEntity(Guid tenantId)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId cannot be empty", nameof(tenantId));
            
        TenantId = tenantId;
    }
}

using Terminar.SharedKernel.ValueObjects;

namespace Terminar.Api.Middleware;

public interface ITenantContext
{
    TenantId TenantId { get; }
    bool IsResolved { get; }
}

public sealed class TenantContext : ITenantContext
{
    private TenantId? _tenantId;

    public TenantId TenantId => _tenantId ?? throw new InvalidOperationException("Tenant context has not been resolved.");
    public bool IsResolved => _tenantId is not null;

    public void SetTenantId(TenantId tenantId) => _tenantId = tenantId;
}

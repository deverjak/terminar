using System.Security.Claims;
using Terminar.SharedKernel.ValueObjects;

namespace Terminar.Api.Middleware;

public sealed class TenantResolutionMiddleware(RequestDelegate next)
{
    private const string TenantIdHeader = "X-Tenant-Id";

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
    {
        var mutableContext = (TenantContext)tenantContext;

        // Skip tenant resolution for auth endpoints and system-admin endpoints
        var path = context.Request.Path.Value ?? string.Empty;
        if (path.StartsWith("/api/v1/auth/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/health", StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        // Priority 1: JWT claim (authenticated staff)
        var tenantClaim = context.User?.FindFirstValue("tenant_id");
        if (tenantClaim is not null && Guid.TryParse(tenantClaim, out var tenantGuidFromClaim))
        {
            mutableContext.SetTenantId(TenantId.From(tenantGuidFromClaim));
            await next(context);
            return;
        }

        // Priority 2: X-Tenant-Id header (public endpoints)
        if (context.Request.Headers.TryGetValue(TenantIdHeader, out var headerValue))
        {
            var headerStr = headerValue.ToString();
            if (Guid.TryParse(headerStr, out var tenantGuidFromHeader))
            {
                mutableContext.SetTenantId(TenantId.From(tenantGuidFromHeader));
                await next(context);
                return;
            }
        }

        // Tenant not resolved — continue (endpoint handlers that require tenant will check IsResolved)
        await next(context);
    }
}

using MediatR;
using Terminar.SharedKernel.ValueObjects;

namespace Terminar.Modules.Tenants.Application.Queries.GetTenant;

public sealed record GetTenantQuery(TenantId TenantId) : IRequest<GetTenantResult>;

public sealed record GetTenantResult(
    Guid TenantId,
    string Name,
    string Slug,
    string DefaultLanguageCode,
    string Status,
    DateTime CreatedAt);

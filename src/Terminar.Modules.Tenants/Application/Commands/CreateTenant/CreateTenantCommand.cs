using MediatR;

namespace Terminar.Modules.Tenants.Application.Commands.CreateTenant;

public sealed record CreateTenantCommand(
    string Name,
    string Slug,
    string DefaultLanguageCode) : IRequest<CreateTenantResult>;

public sealed record CreateTenantResult(Guid TenantId, string Name, string Slug, DateTimeOffset CreatedAt);

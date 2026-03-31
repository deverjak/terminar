using MediatR;
using Microsoft.AspNetCore.Mvc;
using Terminar.Api.Middleware;
using Terminar.Modules.Identity.Application.Auth.Login;
using Terminar.Modules.Identity.Application.Auth.RefreshToken;
using Terminar.Modules.Identity.Application.Commands.CreateStaffUser;
using Terminar.Modules.Identity.Application.Commands.DeactivateStaffUser;
using Terminar.Modules.Identity.Application.Queries.ListStaffUsers;
using Terminar.SharedKernel.ValueObjects;

namespace Terminar.Api.Modules;

public static class IdentityModule
{
    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder app)
    {
        // Auth endpoints — no auth required
        var auth = app.MapGroup("/api/auth").WithTags("Auth");

        auth.MapPost("/login", async (
            [FromBody] LoginRequest req,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(new LoginCommand(req.Username, req.Password), ct);
            return Results.Ok(result);
        });

        auth.MapPost("/refresh", async (
            [FromBody] RefreshTokenRequest req,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(new RefreshTokenCommand(req.UserId.ToString(), req.RefreshToken), ct);
            return Results.Ok(result);
        });

        // Staff management — admin only
        var staff = app.MapGroup("/api/staff")
            .RequireAuthorization("AdminOnly")
            .WithTags("Staff");

        staff.MapGet("/", async (
            ITenantContext tenantCtx,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var tenantId = tenantCtx.TenantId ?? throw new UnauthorizedAccessException("Tenant not resolved.");
            var result = await mediator.Send(new ListStaffUsersQuery(tenantId), ct);
            return Results.Ok(result);
        });

        staff.MapPost("/", async (
            [FromBody] CreateStaffUserRequest req,
            ITenantContext tenantCtx,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var tenantId = tenantCtx.TenantId ?? throw new UnauthorizedAccessException("Tenant not resolved.");
            var result = await mediator.Send(
                new CreateStaffUserCommand(tenantId, req.Username, req.Email, req.Password, req.Role), ct);
            return Results.Created($"/api/staff/{result.StaffUserId}", result);
        });

        staff.MapPost("/{id:guid}/deactivate", async (
            Guid id,
            IMediator mediator,
            CancellationToken ct) =>
        {
            await mediator.Send(new DeactivateStaffUserCommand(id), ct);
            return Results.NoContent();
        });

        return app;
    }
}

public sealed record LoginRequest(string Username, string Password);
public sealed record RefreshTokenRequest(Guid UserId, string RefreshToken);
public sealed record CreateStaffUserRequest(string Username, string Email, string Password, string Role);

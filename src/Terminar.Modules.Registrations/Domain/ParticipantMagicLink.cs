using System.Security.Cryptography;
using Terminar.SharedKernel;
using Terminar.SharedKernel.ValueObjects;

namespace Terminar.Modules.Registrations.Domain;

public sealed class ParticipantMagicLink : AggregateRoot<Guid>
{
    public TenantId TenantId { get; private set; } = default!;
    public Email ParticipantEmail { get; private set; } = default!;
    public string MagicLinkToken { get; private set; } = string.Empty;
    public DateTime MagicLinkExpiresAt { get; private set; }
    public DateTime? MagicLinkUsedAt { get; private set; }
    public string? PortalToken { get; private set; }
    public DateTime? PortalTokenExpiresAt { get; private set; }

    public bool IsMagicLinkValid => MagicLinkUsedAt is null && DateTime.UtcNow < MagicLinkExpiresAt;
    public bool IsPortalTokenValid => PortalToken is not null && PortalTokenExpiresAt.HasValue && DateTime.UtcNow < PortalTokenExpiresAt.Value;

    private ParticipantMagicLink() { }

    public static ParticipantMagicLink Create(TenantId tenantId, Email participantEmail)
    {
        ArgumentNullException.ThrowIfNull(tenantId);
        ArgumentNullException.ThrowIfNull(participantEmail);

        return new ParticipantMagicLink
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ParticipantEmail = participantEmail,
            MagicLinkToken = GenerateToken(),
            MagicLinkExpiresAt = DateTime.UtcNow.AddMinutes(15)
        };
    }

    public string Redeem()
    {
        if (!IsMagicLinkValid)
            throw new UnprocessableException("Magic link is expired or already used.");

        var portalToken = GenerateToken();
        MagicLinkUsedAt = DateTime.UtcNow;
        PortalToken = portalToken;
        PortalTokenExpiresAt = DateTime.UtcNow.AddDays(7);
        return portalToken;
    }

    private static string GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }
}

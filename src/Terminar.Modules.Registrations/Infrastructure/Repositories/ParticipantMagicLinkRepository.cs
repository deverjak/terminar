using Microsoft.EntityFrameworkCore;
using Terminar.Modules.Registrations.Domain;
using Terminar.Modules.Registrations.Domain.Repositories;

namespace Terminar.Modules.Registrations.Infrastructure.Repositories;

public sealed class ParticipantMagicLinkRepository(RegistrationsDbContext db) : IParticipantMagicLinkRepository
{
    public async Task<ParticipantMagicLink?> GetByMagicLinkTokenAsync(string token, CancellationToken ct = default)
        => await db.ParticipantMagicLinks.FirstOrDefaultAsync(x => x.MagicLinkToken == token, ct);

    public async Task<ParticipantMagicLink?> GetByPortalTokenAsync(string token, CancellationToken ct = default)
        => await db.ParticipantMagicLinks.FirstOrDefaultAsync(x => x.PortalToken == token, ct);

    public async Task AddAsync(ParticipantMagicLink magicLink, CancellationToken ct = default)
        => await db.ParticipantMagicLinks.AddAsync(magicLink, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}

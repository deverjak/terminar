namespace Terminar.Modules.Registrations.Domain.Repositories;

public interface IParticipantMagicLinkRepository
{
    Task<ParticipantMagicLink?> GetByMagicLinkTokenAsync(string token, CancellationToken ct = default);
    Task<ParticipantMagicLink?> GetByPortalTokenAsync(string token, CancellationToken ct = default);
    Task AddAsync(ParticipantMagicLink magicLink, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

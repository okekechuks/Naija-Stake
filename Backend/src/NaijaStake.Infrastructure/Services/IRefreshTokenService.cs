using NaijaStake.Domain.Entities;

namespace NaijaStake.Infrastructure.Services;

public interface IRefreshTokenService
{
    Task<RefreshToken> CreateAsync(Guid userId, TimeSpan ttl, CancellationToken cancellationToken = default);
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);
    Task RevokeAsync(Guid refreshTokenId, CancellationToken cancellationToken = default);
}

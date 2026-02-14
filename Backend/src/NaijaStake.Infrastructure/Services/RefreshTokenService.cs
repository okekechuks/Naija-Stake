using NaijaStake.Domain.Entities;
using NaijaStake.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace NaijaStake.Infrastructure.Services;

public class RefreshTokenService : IRefreshTokenService
{
    private readonly StakeItDbContext _context;

    public RefreshTokenService(StakeItDbContext context)
    {
        _context = context;
    }

    public async Task<RefreshToken> CreateAsync(Guid userId, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()) + Guid.NewGuid();
        var expiresAt = DateTime.UtcNow.Add(ttl);
        var rt = RefreshToken.Create(userId, token, expiresAt);
        _context.RefreshTokens.Add(rt);
        await _context.SaveChangesAsync(cancellationToken);
        return rt;
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return await _context.RefreshTokens.FirstOrDefaultAsync(r => r.Token == token, cancellationToken);
    }

    public async Task RevokeAsync(Guid refreshTokenId, CancellationToken cancellationToken = default)
    {
        var rt = await _context.RefreshTokens.FindAsync(new object?[] { refreshTokenId }, cancellationToken: cancellationToken);
        if (rt == null) return;
        rt.Revoke();
        await _context.SaveChangesAsync(cancellationToken);
    }
}

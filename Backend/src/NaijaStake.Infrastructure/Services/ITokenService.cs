namespace NaijaStake.Infrastructure.Services;

public interface ITokenService
{
    string GenerateToken(Guid userId, string email, string firstName, string lastName);
}

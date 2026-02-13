namespace NaijaStake.API.Dtos;

public record CreateWalletRequestDto(Guid UserId);
public record WalletResponseDto(Guid Id, Guid UserId, decimal Available, decimal Locked);

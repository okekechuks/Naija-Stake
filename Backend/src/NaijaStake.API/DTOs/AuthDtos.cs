namespace NaijaStake.API.Dtos;

public record AuthResponseDto(string AccessToken, DateTime ExpiresAt);

public record RefreshDto(string Token, DateTime ExpiresAt);

public record LoginResponseDto(AuthResponseDto Access, RefreshDto Refresh);

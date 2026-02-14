namespace NaijaStake.API.Dtos;

public record RefreshRequestDto(string RefreshToken);

public record RefreshResponseDto(string AccessToken, System.DateTime ExpiresAt, string RefreshToken, System.DateTime RefreshExpiresAt);

public record RevokeRequestDto(string RefreshToken);

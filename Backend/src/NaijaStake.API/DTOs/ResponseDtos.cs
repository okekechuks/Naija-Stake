namespace NaijaStake.API.DTOs;

/// <summary>
/// Standard API error response.
/// </summary>
public class ErrorResponse
{
    public string Code { get; set; } = null!;
    public string Message { get; set; } = null!;
    public string? Details { get; set; }
    public Dictionary<string, string[]>? ValidationErrors { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Standard API success response.
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
}

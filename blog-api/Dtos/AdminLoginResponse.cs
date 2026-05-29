namespace BlogApi.Dtos;

public record AdminLoginResponse(string Token, DateTime ExpiresAt);

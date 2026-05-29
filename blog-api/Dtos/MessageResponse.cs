namespace BlogApi.Dtos;

public record MessageResponse(
    long Id,
    string Name,
    string Email,
    string Content,
    DateTime CreatedAt,
    bool IsRead);

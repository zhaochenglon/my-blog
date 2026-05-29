namespace BlogApi.Dtos;

public record PagedMessageResponse(
    IReadOnlyList<MessageResponse> Items,
    int TotalCount,
    int Page,
    int PageSize);

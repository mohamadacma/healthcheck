namespace HealthCheckApi.DTOs;

public record PagedResponse<T>(
    IEnumerable<T> Data,
    int Page,
    int PageSize,
    int TotalCount);


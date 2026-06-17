namespace PortalRRHHFZ.Application.Common;

public sealed record ApiResponse<T>(bool Success, string Message, T? Data)
{
    public static ApiResponse<T> Ok(T data, string message = "success") => new(true, message, data);
    public static ApiResponse<T> Fail(string message) => new(false, message, default);
}

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Total,
    int Page,
    int PageSize);

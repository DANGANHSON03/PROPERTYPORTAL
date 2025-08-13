namespace PropertyPortal.Models;

public record ApiResult<T>(
    bool Success,
    string Message,
    T? Data,
    object? Errors,
    object? Meta
)
{
    public static ApiResult<T> Ok(T data, string message = "Thành công", object? meta = null)
        => new(true, message, data, null, meta);

    public static ApiResult<T> Fail(string message, object? errors = null, object? meta = null)
        => new(false, message, default, errors, meta);
}

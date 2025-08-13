namespace PropertyPortal.Common;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = "OK";
    public T? Data { get; set; }
    public object? Errors { get; set; }
    public object? Meta { get; set; }

    public static ApiResponse<T> Ok(T data, string message = "Thành công", object? meta = null)
        => new() { Success = true, Message = message, Data = data, Meta = meta };

    public static ApiResponse<T> Fail(string message, object? errors = null, object? meta = null)
        => new() { Success = false, Message = message, Errors = errors, Meta = meta };
}

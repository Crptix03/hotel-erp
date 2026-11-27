namespace HotelErp.Api.Services;

public record ServiceResult<T>(T? Value, int StatusCode, string? Message)
{
    public bool Success => Message is null;

    public static ServiceResult<T> Ok(T value) => new(value, 200, null);
    public static ServiceResult<T> Fail(int statusCode, string message) => new(default, statusCode, message);
}
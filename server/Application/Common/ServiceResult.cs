namespace server.Application.Common;

public class ServiceResult<T>
{
    public bool IsSuccess { get; private init; }
    public T? Data { get; private init; }
    public string? ErrorMessage { get; private init; }
    public string? ErrorCode { get; private init; }

    public static ServiceResult<T> Success(T data) =>
        new() { IsSuccess = true, Data = data };

    public static ServiceResult<T> Failure(string message, string? code = null) =>
        new() { IsSuccess = false, ErrorMessage = message, ErrorCode = code };
}

public static class ServiceResult
{
    public static ServiceResult<bool> Ok() => ServiceResult<bool>.Success(true);
    public static ServiceResult<bool> Fail(string message, string? code = null) =>
        ServiceResult<bool>.Failure(message, code);
}

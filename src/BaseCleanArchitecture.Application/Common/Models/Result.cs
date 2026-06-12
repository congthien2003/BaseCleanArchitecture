namespace BaseCleanArchitecture.Application.Common.Models;

public interface IResult
{
    int StatusCode { get; }
    bool IsSuccess { get; }
    string? Message { get; }
}

public interface IResult<out T> : IResult
{
    T? Data { get; }
}

public sealed record Result : IResult
{
    public int StatusCode { get; }
    public bool IsSuccess { get; }
    public string? Message { get; }

    private Result(int statusCode, bool isSuccess, string? message)
    {
        StatusCode = statusCode;
        IsSuccess = isSuccess;
        Message = message;
    }

    public static Result Success(int statusCode = 200, string? message = null)
        => new(statusCode, true, message);

    public static Result Failure(int statusCode, string? message)
        => new(statusCode, false, message);
}

public sealed record Result<T> : IResult<T>
{
    public int StatusCode { get; }
    public bool IsSuccess { get; }
    public string? Message { get; }
    public T? Data { get; }

    private Result(int statusCode, bool isSuccess, T? data, string? message)
    {
        StatusCode = statusCode;
        IsSuccess = isSuccess;
        Message = message;
        Data = data;
    }

    public static Result<T> Success(T data, int statusCode = 200, string? message = null)
        => new(statusCode, true, data, message);

    public static Result<T> Failure(int statusCode, string? message)
        => new(statusCode, false, default, message);
}

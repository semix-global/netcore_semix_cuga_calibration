namespace Core.Algorithm.Interfaces;

/// <summary>
/// 算法 API 通用结果包装
/// </summary>
public sealed class AlgorithmResultRet<T>
{
    public bool Success { get; init; }

    public string ErrorMsg { get; init; } = "";

    public T? Data { get; init; }
}

public static class AlgorithmResultRet
{
    public static AlgorithmResultRet<T> CreateSuccess<T>(T data) =>
        new()
        {
            Success = true,
            Data = data
        };

    public static AlgorithmResultRet<T> CreateError<T>(string errorMsg, T? data = default) =>
        new()
        {
            Success = false,
            ErrorMsg = errorMsg,
            Data = data
        };
}
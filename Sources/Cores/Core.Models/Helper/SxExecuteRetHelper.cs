using Semix.CoreLib;

namespace Core.Models.Helper;

public static class SxExecuteRetHelper
{
    public static SxExecuteRet<T> Create<T>(bool success, string msg, T data)
    {
        return new SxExecuteRet<T>
        {
            Success = success,
            Msg = msg,
            Anything = data
        };
    }

    public static SxExecuteRet<T> CreateSuccess<T>(T data)
    {
        return new SxExecuteRet<T>
        {
            Success = true,
            Anything = data
        };
    }

    public static SxExecuteRet CreateSuccess()
    {
        return new SxExecuteRet
        {
            Success = true,
#if NETFRAMEWORK
            Anything = new object()
#endif
        };
    }

    public static SxExecuteRet CreateSuccess(object obj)
    {
        return new SxExecuteRet
        {
            Success = true,
#if NETFRAMEWORK
            Anything = new object()
#endif
        };
    }

    public static SxExecuteRet<T> CreateError<T>(string msg) where T : new()
    {
        return new SxExecuteRet<T>
        {
            Success = false,
            Msg = msg,
            Anything = new T()
        };
    }

    public static SxExecuteRet<T> CreateError<T>(string msg, T data)
    {
        return new SxExecuteRet<T>
        {
            Success = false,
            Msg = msg,
            Anything = data
        };
    }
}
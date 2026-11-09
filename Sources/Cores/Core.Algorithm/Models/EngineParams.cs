namespace Core.Algorithm.Models;

/// <summary>
/// 引擎参数基类：所有引擎接口入参的通用类型，各算法库专属参数继承本类，经 EngineParams 槽位传递给引擎
/// </summary>
public abstract class EngineParams
{
    /// <summary>
    /// 引擎参数类型防呆：喂错引擎的参数时返回明确错误
    /// </summary>
    /// <typeparam name="T">引擎期望的参数类型</typeparam>
    /// <param name="engineParams">引擎参数（可为 null，null 时返回默认实例）</param>
    /// <param name="value">取出的参数</param>
    /// <param name="errorMsg">错误信息</param>
    /// <returns>是否成功</returns>
    public static bool TryGet<T>(EngineParams? engineParams, out T value, out string errorMsg)
        where T : EngineParams, new()
    {
        switch (engineParams)
        {
            case null:
                value = new T();
                errorMsg = string.Empty;
                return true;
            case T typed:
                value = typed;
                errorMsg = string.Empty;
                return true;
            default:
                value = new T();
                errorMsg = $"EngineParams must be {typeof(T).Name}";
                return false;
        }
    }

    /// <summary>
    /// HtmlLog 匿名输出
    /// </summary>
    /// <returns>匿名对象</returns>
    public abstract object ToHtmlAnonymous();
}

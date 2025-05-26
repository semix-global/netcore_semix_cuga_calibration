using System.Text;
using System.Text.RegularExpressions;

namespace CanvasViewer.Editor.Entity.Options;

/// <summary>
/// 编辑器输入选项
/// </summary>
/// <typeparam name="TInput">输入结果类型</typeparam>
public abstract class AbstractInputOptions<TInput>(string message, Action<TInput> jig) where TInput : new()
{
    /// <summary>
    /// 匹配小写字符正则表达式(^表示非)
    /// </summary>
    // ReSharper disable once StaticMemberInGenericType
#pragma warning disable IDE0079
#pragma warning disable SYSLIB1045
    private static readonly Regex UpperOnly = new("[^A-Z]", RegexOptions.Compiled);
#pragma warning restore SYSLIB1045
#pragma warning restore IDE0079

    #region 属性

    /// <summary>
    /// 编辑器提示符信息前缀
    /// </summary>
    public string Message { get; set; } = message;

    /// <summary>
    /// 显示抖动回调抖动值(编辑的时候添加点可移动就是抖动的[比如polyline中后续的点就是抖动的点, 回调的点设置到polyline最后一个点位置])
    /// </summary>
    public Action<TInput> Jig { get; private set; } = jig;

    /// <summary>
    /// 关键字(类似于Cad里面的命令)命令列表
    /// </summary>
    internal List<string> Keywords { get; private set; } = [];

    /// <summary>
    /// 关键字(类似于Cad里面的命令)命令别名列表
    /// </summary>
    internal List<string> Aliases { get; private set; } = [];

    /// <summary>
    /// 默认关键字(类似于Cad里面的命令)
    /// </summary>
    internal string DefaultKeyword { get; private set; } = "";

    #endregion 属性

    #region 构造

    protected AbstractInputOptions(string message) : this(message, _ => { })
    {
    }

    #endregion 构造

    #region 方法

    /// <summary>
    /// 添加关键字(类似于Cad里面的命令)
    /// </summary>
    /// <param name="keyword">关键字(类似于Cad里面的命令)</param>
    /// <param name="isDefault">是否默认</param>
    public void AddKeyword(string keyword, bool isDefault = false)
    {
        Keywords.Add(keyword);
        var alias = UpperOnly.Replace(keyword, ""); // 去掉小写字母
        Aliases.Add(alias);

        if (isDefault) SetDefaultKeyword(keyword);
    }

    /// <summary>
    /// 设置默认关键字(类似于Cad里面的命令)
    /// </summary>
    /// <param name="keyword">默认关键字(类似于Cad里面的命令)</param>
    internal void SetDefaultKeyword(string keyword)
    {
        DefaultKeyword = keyword;
    }

    /// <summary>
    /// 匹配关键字(类似于Cad里面的命令)并返回关键字(类似于Cad里面的命令)
    /// </summary>
    /// <param name="input">输入</param>
    /// <returns>关键字(类似于Cad里面的命令)</returns>
    internal string MatchKeyword(string input)
    {
        if (string.IsNullOrEmpty(input) && string.IsNullOrEmpty(DefaultKeyword) == false) return DefaultKeyword;
        if (string.IsNullOrEmpty(input)) return string.Empty;

        for (var i = 0; i < Aliases.Count; i++)
        {
            if (string.Compare(Aliases[i], input, StringComparison.OrdinalIgnoreCase) == 0) return Keywords[i];
        }

        foreach (var t in Keywords.Where(t => t.StartsWith(input, StringComparison.OrdinalIgnoreCase)))
            return t;

        return string.Empty;
    }

    /// <summary>
    /// 获取完整编辑器提示符
    /// </summary>
    /// <returns>编辑器提示符(包含命令)</returns>
    internal virtual string GetFullPrompt()
    {
        if (Keywords.Count == 0) return $"{Message.TrimEnd(' ', ':')}: ";

        var sb = new StringBuilder(Message.TrimEnd(' ', ':'));
        sb.Append(" [");
        sb.Append(string.Join(", ", Keywords));
        sb.Append("] ");
        if (Keywords.Contains(DefaultKeyword))
        {
            sb.Append(" <");
            sb.Append(DefaultKeyword);
            sb.Append("> ");
        }

        sb.Append(": ");
        return sb.ToString();
    }

    #endregion 方法
}
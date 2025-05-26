using CanvasViewer.Document;
using CanvasViewer.Editor.Entity;
using CanvasViewer.Editor.Entity.Getter;
using CanvasViewer.Editor.Enum;
using CanvasViewer.Media.EventArg;
using System.Windows.Input;

namespace CanvasViewer.Editor;

public sealed class CanvasEditor(CanvasDocument doc)
{
    #region 事件

    /// <summary>
    /// 编辑提示符事件
    /// </summary>
    public event EditorPromptEventHandler? Prompt;

    /// <summary>
    /// 编辑错误事件
    /// </summary>
    public event EditorErrorEventHandler? Error;

    /// <summary>
    /// 光标移动事件
    /// </summary>
    internal event CursorEventHandler? CursorMove;

    /// <summary>
    /// 光标点击事件
    /// </summary>
    internal event CursorEventHandler? CursorClick;

    /// <summary>
    /// 键盘按下任意键事件
    /// </summary>
    internal event KeyEventHandler? KeyDown;

    #endregion 事件

    #region 属性

    /// <summary>
    /// 文本
    /// </summary>
    public CanvasDocument Document { get; private set; } = doc;

    /// <summary>
    /// 鼠标点击挑选的Drawable对象集合
    /// </summary>
    public SelectionSet PickedSelection { get; private set; } = [];

    /// <summary>
    /// 编辑时候捕捉的锚点类型
    /// </summary>
    public SnapPointTypeEnum SnapMode => Document.Settings.SnapMode;

    /// <summary>
    /// 是否是编辑模式
    /// </summary>
    public bool IsInputMode { get; internal set; }

    /// <summary>
    /// 命令是否执行
    /// </summary>
    public bool IsCommandInProgress { get; internal set; }

    /// <summary>
    /// 编辑时候捕捉的锚点集合
    /// </summary>
    internal SnapPointCollection SnapPoints { get; set; } = new();

    /// <summary>
    /// 上一个命令名称
    /// </summary>
    internal string LastCommandName { get; private set; } = string.Empty;

    /// <summary>
    /// 上一个命令参数
    /// </summary>
    internal string[] LastCommandArgs { get; private set; } = [];

    #endregion 属性

    #region View Events

    internal void OnViewMouseMove(object sender, CursorEventArgs e)
    {
        if (IsCommandInProgress) CursorMove?.Invoke(sender, e);
    }

    internal void OnViewMouseClick(object sender, CursorEventArgs e)
    {
        if (IsCommandInProgress) CursorClick?.Invoke(sender, e);
    }

    internal void OnViewKeyDown(object sender, KeyEventArgs e)
    {
        if (IsCommandInProgress) KeyDown?.Invoke(sender, e);
    }

#pragma warning disable IDE0051 // 删除未使用的私有成员

    private void OnPrompt(EditorPromptEventArgs e)
#pragma warning restore IDE0051 // 删除未使用的私有成员
    {
        Prompt?.Invoke(this, e);
    }

#pragma warning disable IDE0051 // 删除未使用的私有成员

    private void OnError(EditorErrorEventArgs e)
#pragma warning restore IDE0051 // 删除未使用的私有成员
    {
        Error?.Invoke(this, e);
    }

    #endregion View Events
}
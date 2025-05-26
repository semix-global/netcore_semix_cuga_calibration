using CanvasViewer.Drawables;
using CanvasViewer.Editor;
using CanvasViewer.Media.Drawing;
using CanvasViewer.Media.EventArg;
using CanvasViewer.View;
using System.Collections.Specialized;

namespace CanvasViewer.Document;

public sealed class CanvasDocument
{
    #region 事件

    /// <summary>
    /// 文档改变事件
    /// </summary>
    public event DocumentChangedEventHandler? DocumentChanged;

    /// <summary>
    /// 图形抖动变换或者产生瞬时图形事件
    /// </summary>
    public event TransientsChangedEventHandler? TransientsChanged;

    /// <summary>
    /// 文档选择事件
    /// </summary>
    public event SelectionChangedEventHandler? SelectionChanged;

    #endregion 事件

    #region 属性

    /// <summary>
    /// 编辑Drawable对象
    /// </summary>
    public CanvasEditor Editor { get; private set; }

    /// <summary>
    /// 设置
    /// </summary>
    public Settings Settings { get; private set; }

    /// <summary>
    /// Layer字典
    /// </summary>
    public Dictionary<string, Layer> Layers { get; private set; }

    /// <summary>
    /// 文档内容
    /// </summary>
    public Model Model { get; private set; }

    /// <summary>
    /// 用于显示抖动(用于编辑器显示抖动图形, 编辑的时候添加点可移动就是抖动的[比如polyline中后续的点就是抖动的点])
    /// </summary>
    public Composite Jigged { get; private set; }

    /// <summary>
    /// 用于瞬时显示(比如选择框)
    /// </summary>
    public Composite Transients { get; private set; }

    /// <summary>
    /// 是否修改
    /// </summary>
    public bool IsModified { get; private set; }

    /// <summary>
    /// 文档当前视图
    /// </summary>
    public CanvasView? ActiveView { get; set; }

    /// <summary>
    /// 是不是设计模式
    /// </summary>
    public bool IsInDesignMode { get; set; }

    #endregion 属性

    #region 构造

    public CanvasDocument()
    {
        Editor = new CanvasEditor(this);

        Settings = new Settings();
        Layers = [];

        Model = new Model(this);
        Jigged = [];
        Transients = [];

        IsModified = false;

        ActiveView = null;

        Editor.PickedSelection.CollectionChanged += SelectionOnCollectionChanged;
        Model.CollectionChanged += ModelOnCollectionChanged;
        Jigged.CollectionChanged += JiggedOnCollectionChanged;
    }

    #endregion 构造

    #region 方法

    public void New()
    {
        Settings.Reset();
        Layers.Clear();

        Model.Clear();
        Jigged.Clear();
        Transients.Clear();

        IsModified = false;

        Editor.PickedSelection.Clear();
    }

    public void Clear()
    {
        Layers.Clear();

        Model.Clear();
        Jigged.Clear();
        Transients.Clear();
        ClearSelect();

        IsModified = false;
    }

    public void ClearSelect()
    {
        Editor.PickedSelection.Clear();
    }

    public void BeginEdit()
    {
        IsInDesignMode = true;
    }

    public void EndEdit()
    {
        IsInDesignMode = false;
    }

    #endregion 方法

    #region 调用事件

    private void OnDocumentChanged(EventArgs e)
    {
        IsModified = true;
        if (IsInDesignMode) return;
        DocumentChanged?.Invoke(this, e);
    }

    private void OnTransientsChanged(EventArgs e)
    {
        if (IsInDesignMode) return;
        TransientsChanged?.Invoke(this, e);
    }

    private void OnSelectionChanged(NotifyCollectionChangedEventArgs e)
    {
        if (IsInDesignMode) return;
        SelectionChanged?.Invoke(this, e);
    }

    private void ModelOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                OnDocumentChanged(EventArgs.Empty);
                break;

            case NotifyCollectionChangedAction.Remove:
                OnDocumentChanged(EventArgs.Empty);
                break;

            case NotifyCollectionChangedAction.Reset:
                OnDocumentChanged(EventArgs.Empty);
                break;

            case NotifyCollectionChangedAction.Move:
            case NotifyCollectionChangedAction.Replace:
                OnDocumentChanged(EventArgs.Empty);
                break;
        }
    }

    private void JiggedOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnTransientsChanged(EventArgs.Empty);
    }

    private void SelectionOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnSelectionChanged(e);
    }

    #endregion 调用事件
}
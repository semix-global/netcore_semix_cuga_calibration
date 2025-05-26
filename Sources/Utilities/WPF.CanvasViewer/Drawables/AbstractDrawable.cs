using CanvasViewer.Editor.Entity;
using CanvasViewer.Geometry;
using CanvasViewer.Media;
using CanvasViewer.Media.Drawing;
using System.ComponentModel;
using System.Runtime.CompilerServices;

// ReSharper disable MemberCanBeProtected.Global

namespace CanvasViewer.Drawables;

public abstract class AbstractDrawable : INotifyPropertyChanged
{
    #region 属性

    /// <summary>
    /// 样式
    /// </summary>
    public Style Style { get; set; } = Style.Default;

    /// <summary>
    /// 图层
    /// </summary>
    public Layer Layer { get; set; } = Layer.Default;

    /// <summary>
    /// 是否显示
    /// </summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// 是否加入List
    /// </summary>
    internal bool IsInModel { get; set; }

    /// <summary>
    /// 唯一性索引
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// Tag
    /// </summary>
    public object? Tag { get; set; }

    #endregion 属性

    #region abstract方法

    /// <summary>
    /// 绘画
    /// </summary>
    public abstract void Draw(Renderer renderer);

    /// <summary>
    /// 获取区域
    /// </summary>
    public abstract Extents2D GetExtents();

    /// <summary>
    /// 绘画变换
    /// </summary>
    public abstract void TransformBy(Matrix2D transformation);

    #endregion abstract方法

    #region 覆盖方法

    /// <summary>
    /// 是否包含该点
    /// </summary>
    /// <param name="pt">点</param>
    /// <param name="pickBoxSize">范围</param>
    /// <returns>是否包含</returns>
    public virtual bool Contains(Point2D pt, double pickBoxSize)
    {
        return GetExtents().Contains(pt);
    }

    /// <summary>
    /// 获取控制锚点
    /// </summary>
    public virtual ControlPoint[] GetControlPoints()
    {
        return [];
    }

    /// <summary>
    /// 获取编辑时候捕捉的锚点
    /// </summary>
    public virtual SnapPoint[] GetSnapPoints()
    {
        return [];
    }

    /// <summary>
    /// 控制锚点变换
    /// </summary>
    /// <param name="indices">需要变换的索引集合</param>
    /// <param name="transformation">变换矩阵</param>
    public virtual void TransformControlPoints(int[] indices, Matrix2D transformation)
    {
    }

    public virtual AbstractDrawable Clone()
    {
        return (AbstractDrawable)MemberwiseClone();
    }

    #endregion 覆盖方法

    #region PropertyChanged event

    [field: NonSerialized]
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    #endregion PropertyChanged event
}
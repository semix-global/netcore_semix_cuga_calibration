using CanvasViewer.Drawables;
using CanvasViewer.Editor.Enum;
using CanvasViewer.Geometry;

namespace CanvasViewer.Editor.Entity;

public sealed class SnapPointCollection
{
    private readonly SortedList<SnapPointEntry, bool> _items = [];

    /// <summary>
    /// 当前集合的索引位置(指针)
    /// </summary>
    private int _currentIndex;

    #region 属性

    public bool IsEmpty => _items.Count == 0;

    #endregion 属性

    #region 方法

    /// <summary>
    /// 添加锚点
    /// </summary>
    public void Add(double distance, SnapPoint point)
    {
        _items.Add(new SnapPointEntry(distance, point), false);
    }

    /// <summary>
    /// 添加锚点
    /// </summary>
    public void AddFromDrawable(AbstractDrawable item, Point2D cursorLocation, SnapPointTypeEnum snapMode, double snapDistance)
    {
        foreach (var pt in item.GetSnapPoints())
        {
            if ((snapMode & pt.Type) != pt.Type) continue; // 排序不满足条件的锚点

            var dist = (pt.Location - cursorLocation).Length;
            if (dist <= snapDistance) Add(dist, pt);
        }
    }

    /// <summary>
    /// 清除锚点集合
    /// </summary>
    public void Clear()
    {
        _items.Clear();
        _currentIndex = 0;
    }

    /// <summary>
    /// 下一个锚点
    /// </summary>
    public void Next()
    {
        _currentIndex++;
        if (_currentIndex == _items.Count) _currentIndex = 0;
    }

    /// <summary>
    /// 上一个锚点
    /// </summary>
    public void Previous()
    {
        _currentIndex--;
        if (_currentIndex == -1) _currentIndex = _items.Count - 1;
    }

    /// <summary>
    /// 当前锚点
    /// </summary>
    public SnapPoint Current()
    {
        return _items.Keys[_currentIndex].SnapPoint;
    }

    #endregion 方法

    /// <summary>
    /// 显示转换锚点集合(当前点)到Point2D
    /// </summary>
    public static implicit operator Point2D(SnapPointCollection collection)
    {
        return collection.Current().Location;
    }

    private sealed class SnapPointEntry(double distance, SnapPoint snapPoint) : IComparable<SnapPointEntry>
    {
        /// <summary>
        /// 编辑时候捕捉的锚点
        /// </summary>
        public SnapPoint SnapPoint { get; } = snapPoint;

        /// <summary>
        /// 档期锚点与一个定点(一般为光标位置)的距离
        /// </summary>
        private readonly double _distance = distance;

        public int CompareTo(SnapPointEntry? other)
        {
#if NET
            ArgumentNullException.ThrowIfNull(nameof(other));
#else
            if (other is null) throw new ArgumentNullException(nameof(other));
#endif

#if NET
            var distCmp = _distance.CompareTo(other!._distance);
#else
            var distCmp = _distance.CompareTo(other._distance);
#endif
            if (distCmp == 0)
                return (int)SnapPoint.Type < (int)other.SnapPoint.Type ? -1 : 1;

            return distCmp;
        }
    }
}
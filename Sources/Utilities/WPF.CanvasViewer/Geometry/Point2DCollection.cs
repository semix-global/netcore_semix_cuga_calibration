using System.Collections;
using System.Collections.Specialized;
using System.Windows;

namespace CanvasViewer.Geometry;

/// <summary>
/// 点集合
/// </summary>
public sealed class Point2DCollection : IList<Point2D>, INotifyCollectionChanged
{
    private readonly List<Point2D> _items;

    #region 属性

    public int Count => _items.Count;

    public bool IsReadOnly => false;

    #endregion 属性

    #region 构造

    public Point2DCollection()
    {
        _items = [];
    }

    public Point2DCollection(IEnumerable<Point2D> elements)
    {
        _items = [.. elements];
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, _items));
    }

    public Point2DCollection(IEnumerable<Point> elements)
    {
        _items = [];
        foreach (var item in elements)
        {
            _items.Add(new Point2D(item));
        }

        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, _items));
    }

    #endregion 构造

    #region 方法

    /// <summary>
    /// 获取范围RectangleF
    /// </summary>
    public Extents2D GetExtents()
    {
        var ex = new Extents2D();
        foreach (var item in _items) ex.Add(item);

        return ex;
    }

    public Point2D this[int index]
    {
        get => _items[index];
        set
        {
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, _items[index]));
            _items[index] = value;
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, _items[index]));
        }
    }

    public void Add(double x, double y)
    {
        var item = new Point2D(x, y);
        _items.Add(item);
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));
    }

    public void Add(Point2D item)
    {
        _items.Add(item);
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));
    }

    public void AddRange(IEnumerable<Point2D> list)
    {
        _items.AddRange(list);
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, list));
    }

    public void Clear()
    {
        _items.Clear();
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    public bool Contains(Point2D item)
    {
        return _items.Contains(item);
    }

    public void CopyTo(Point2D[] array, int arrayIndex)
    {
        _items.CopyTo(array, arrayIndex);
    }

    public bool Remove(Point2D item)
    {
        var check = _items.Remove(item);
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item));
        return check;
    }

    public Point[] ToPointF()
    {
        var points = new Point[_items.Count];
        for (var i = 0; i < _items.Count; i++)
        {
            points[i] = (Point)_items[i];
        }

        return points;
    }

    public void TransformBy(Matrix2D transformation)
    {
        for (var i = 0; i < _items.Count; i++)
        {
            _items[i] = _items[i].Transform(transformation);
        }

        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    public int IndexOf(Point2D item)
    {
        return _items.IndexOf(item);
    }

    public void Insert(int index, Point2D item)
    {
        _items.Insert(index, item);
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));
    }

    public void RemoveAt(int index)
    {
        _items.RemoveAt(index);
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, _items[index]));
    }

    public IEnumerator<Point2D> GetEnumerator()
    {
        return _items.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    #endregion 方法

    #region NotifyCollectionChangedEventHandler

    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    private void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        CollectionChanged?.Invoke(this, e);
    }

    #endregion NotifyCollectionChangedEventHandler
}
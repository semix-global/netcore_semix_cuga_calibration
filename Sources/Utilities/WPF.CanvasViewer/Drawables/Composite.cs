using CanvasViewer.Editor.Entity;
using CanvasViewer.Geometry;
using CanvasViewer.Media;
using System.Collections.Specialized;
using System.ComponentModel;

namespace CanvasViewer.Drawables;

/// <summary>
/// 混合多个绘画对象
/// </summary>
public class Composite(string name) : AbstractDrawable, ICollection<AbstractDrawable>, INotifyCollectionChanged
{
    private readonly List<AbstractDrawable> _items = [];
    private Extents2D? _compositeExtents2DCache;

    #region 属性

    public string Name { get; set; } = name;
    public virtual int Count => _items.Count;
    public bool IsReadOnly => false;

    #endregion 属性

    #region 构造

    public Composite() : this("Composite")
    {
    }

    #endregion 构造

    #region ICollection

    public virtual void Add(AbstractDrawable item)
    {
        _items.Add(item);
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));
    }

    public virtual void Clear()
    {
        _items.Clear();
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    public virtual bool Contains(AbstractDrawable item)
    {
        return _items.Contains(item);
    }

    public virtual void CopyTo(AbstractDrawable[] array, int arrayIndex)
    {
        _items.CopyTo(array, arrayIndex);
    }

    public virtual bool Remove(AbstractDrawable item)
    {
        var check = _items.Remove(item);
        if (check)
        {
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item));
        }

        return check;
    }

    public virtual IEnumerator<AbstractDrawable> GetEnumerator()
    {
        return _items.GetEnumerator();
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    #endregion ICollection

    #region AbstractDrawable

    public override void Draw(Renderer renderer)
    {
        foreach (var item in _items.Where(item => item.IsVisible && item.Layer.IsVisible))
        {
            renderer.Draw(item);
        }
    }

    public override Extents2D GetExtents()
    {
        if (_compositeExtents2DCache is not null) return _compositeExtents2DCache;

        _compositeExtents2DCache = new Extents2D();
        foreach (var item in _items.Where(item => item.IsVisible && item.Layer.IsVisible))
        {
            _compositeExtents2DCache.Add(item.GetExtents());
        }

        return _compositeExtents2DCache;
    }

    public override bool Contains(Point2D pt, double pickBoxSize)
    {
        return _items.Any(d => d.IsVisible && d.Layer.IsVisible && d.Contains(pt, pickBoxSize));
    }

    public override void TransformBy(Matrix2D transformation)
    {
        foreach (var item in _items)
        {
            item.TransformBy(transformation);
        }
    }

    public override SnapPoint[] GetSnapPoints()
    {
        var points = new List<SnapPoint>();
        foreach (var item in _items.Where(item => item.IsVisible && item.Layer.IsVisible))
        {
            points.AddRange(item.GetSnapPoints());
        }

        return [.. points];
    }

    public override AbstractDrawable Clone()
    {
        var newComposite = (Composite)base.Clone();
        foreach (var d in _items)
        {
            newComposite.Add(d.Clone());
        }

        return newComposite;
    }

    #endregion AbstractDrawable

    #region NotifyCollectionChangedEventHandler

    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        _compositeExtents2DCache = null;
        if (e.NewItems is not null)
        {
            foreach (AbstractDrawable item in e.NewItems)
            {
                item.PropertyChanged += OnCollectionChanged;
            }
        }

        if (e.OldItems is not null)
        {
            foreach (AbstractDrawable item in e.OldItems)
                item.PropertyChanged -= OnCollectionChanged;
        }

        CollectionChanged?.Invoke(this, e);
    }

    private void OnCollectionChanged(object? sender, PropertyChangedEventArgs e)
    {
        _compositeExtents2DCache = null;
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, sender, sender));
    }

    #endregion NotifyCollectionChangedEventHandler
}
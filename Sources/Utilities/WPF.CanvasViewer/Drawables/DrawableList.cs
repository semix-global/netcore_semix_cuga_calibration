using CanvasViewer.Geometry;
using CanvasViewer.Media;
using System.Collections;

namespace CanvasViewer.Drawables;

/// <summary>
/// 绘画列表
/// </summary>
public sealed class DrawableList : AbstractDrawable, IList<AbstractDrawable>
{
    private readonly List<AbstractDrawable> _items = [];

    #region 属性

    public AbstractDrawable this[int index]
    {
        get => _items[index];
        set => _items[index] = value;
    }

    public int Count => _items.Count;

    public bool IsReadOnly => false;

    #endregion 属性

    #region Ilist

    public void Add(AbstractDrawable value)
    {
        _items.Add(value);
    }

    public void Clear()
    {
        _items.Clear();
    }

    public bool Remove(AbstractDrawable item)
    {
        return _items.Remove(item);
    }

    public int IndexOf(AbstractDrawable item)
    {
        return _items.IndexOf(item);
    }

    public void Insert(int index, AbstractDrawable item)
    {
        _items.Insert(index, item);
    }

    public void RemoveAt(int index)
    {
        _items.RemoveAt(index);
    }

    public void CopyTo(AbstractDrawable[] array, int arrayIndex)
    {
        _items.CopyTo(array, arrayIndex);
    }

    public bool Contains(AbstractDrawable item)
    {
        return _items.Contains(item);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public IEnumerator<AbstractDrawable> GetEnumerator()
    {
        return _items.GetEnumerator();
    }

    #endregion Ilist

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
        var extents = new Extents2D();
        foreach (var item in _items.Where(item => item.IsVisible && item.Layer.IsVisible))
        {
            extents.Add(item.GetExtents());
        }

        return extents;
    }

    public override void TransformBy(Matrix2D transformation)
    {
        foreach (var item in _items)
        {
            item.TransformBy(transformation);
        }
    }

    public override bool Contains(Point2D pt, double pickBoxSize)
    {
        return _items.Any(d => d.IsVisible && d.Layer.IsVisible && d.Contains(pt, pickBoxSize));
    }

    public override AbstractDrawable Clone()
    {
        var newList = (DrawableList)base.Clone();
        foreach (var item in _items)
        {
            newList.Add(item.Clone());
        }

        return newList;
    }

    #endregion AbstractDrawable
}
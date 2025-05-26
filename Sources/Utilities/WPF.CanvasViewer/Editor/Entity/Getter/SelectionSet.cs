using CanvasViewer.Drawables;
using System.Collections;
using System.Collections.Specialized;

namespace CanvasViewer.Editor.Entity.Getter;

public sealed class SelectionSet : ISet<AbstractDrawable>, INotifyCollectionChanged
{
    private readonly HashSet<AbstractDrawable> _items = [];

    #region 属性

    public int Count => _items.Count;

    public bool IsReadOnly => false;

    #endregion 属性

    #region IList

    public bool Add(AbstractDrawable item)
    {
        var check = _items.Add(item);
        if (check) OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));

        return check;
    }

    // 显示实现接口
    void ICollection<AbstractDrawable>.Add(AbstractDrawable item)
    {
        Add(item);
    }

    public void Clear()
    {
        _items.Clear();
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    public bool Contains(AbstractDrawable item)
    {
        return _items.Contains(item);
    }

    public void CopyTo(AbstractDrawable[] array, int arrayIndex)
    {
        _items.CopyTo(array, arrayIndex);
    }

    public bool Remove(AbstractDrawable item)
    {
        var check = _items.Remove(item);
        if (check) OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item));

        return check;
    }

    public IEnumerator<AbstractDrawable> GetEnumerator()
    {
        return _items.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    #endregion IList

    #region Iset

    public void UnionWith(IEnumerable<AbstractDrawable> other)
    {
        var result = other.Select(item => _items.Add(item)).ToList();

        if (result.All(b => b)) OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, other));
    }

    #region Not Implemented

    void ISet<AbstractDrawable>.IntersectWith(IEnumerable<AbstractDrawable> other)
    {
        throw new NotImplementedException();
    }

    void ISet<AbstractDrawable>.ExceptWith(IEnumerable<AbstractDrawable> other)
    {
        throw new NotImplementedException();
    }

    void ISet<AbstractDrawable>.SymmetricExceptWith(IEnumerable<AbstractDrawable> other)
    {
        throw new NotImplementedException();
    }

    bool ISet<AbstractDrawable>.IsSubsetOf(IEnumerable<AbstractDrawable> other)
    {
        throw new NotImplementedException();
    }

    bool ISet<AbstractDrawable>.IsSupersetOf(IEnumerable<AbstractDrawable> other)
    {
        throw new NotImplementedException();
    }

    bool ISet<AbstractDrawable>.IsProperSupersetOf(IEnumerable<AbstractDrawable> other)
    {
        throw new NotImplementedException();
    }

    bool ISet<AbstractDrawable>.IsProperSubsetOf(IEnumerable<AbstractDrawable> other)
    {
        throw new NotImplementedException();
    }

    bool ISet<AbstractDrawable>.Overlaps(IEnumerable<AbstractDrawable> other)
    {
        throw new NotImplementedException();
    }

    bool ISet<AbstractDrawable>.SetEquals(IEnumerable<AbstractDrawable> other)
    {
        throw new NotImplementedException();
    }

    #endregion Not Implemented

    #endregion Iset

    #region NotifyCollectionChangedEventHandler

    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    private void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        CollectionChanged?.Invoke(this, e);
    }

    #endregion NotifyCollectionChangedEventHandler
}
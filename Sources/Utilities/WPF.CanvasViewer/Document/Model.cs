using CanvasViewer.Drawables;
using System.Collections.Specialized;

namespace CanvasViewer.Document;

public sealed class Model : Composite
{
    public CanvasDocument Document { get; private set; }

    public Model(CanvasDocument doc)
    {
        Document = doc;
        Name = "<MODEL>";
    }

    #region ICollection

    public override void Add(AbstractDrawable item)
    {
        item.IsInModel = true;
        base.Add(item);
    }

    public override bool Remove(AbstractDrawable item)
    {
        item.IsInModel = false;
        return base.Remove(item);
    }

    #endregion ICollection

    #region NotifyCollectionChangedEventHandler

    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        base.OnCollectionChanged(e);

        if (e.NewItems is null) return;

        foreach (AbstractDrawable item in e.NewItems) CheckOrAddLayer(item); // add layers
    }

    private void CheckOrAddLayer(AbstractDrawable item)
    {
        var layer = item.Layer;
        if (Document.Layers.TryGetValue(layer.Name, out var docLayer)) item.Layer = docLayer;
        else Document.Layers.Add(layer.Name, layer);
    }

    #endregion NotifyCollectionChangedEventHandler
}
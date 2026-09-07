using Net.Utilities.Graphics;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Graphics.Interfaces;
using Net.Utilities.Graphics.Primitives.Medias.Layers;
using Net.Utilities.Graphics.Primitives.ObjectModels;
using Net.Utilities.OpticsFourierImageViewer.WPF.Drawables;

namespace Net.Utilities.OpticsFourierImageViewer.WPF;

public sealed class OpticsFourierImageDocument : CanvasDocument
{
    public readonly ILayer ImageLayer = new ImmutableLayer("Image", 100);
    public readonly ILayer ROILayer = new ImmutableLayer("ROI", 200);

    public Model<BitmapImageDrawable> ImageModel { get; }

    public Model<BitmapImageROIDrawable> ROIModel { get; }

    public OpticsFourierImageDocument()
    {
        ImageModel = ModelStorages.GetOrAddLayer<BitmapImageDrawable>(ImageLayer);
        ROIModel = ModelStorages.GetOrAddLayer<BitmapImageROIDrawable>(ROILayer);

        Settings.SelectionHighlightOverrideFillStyle = null;
        Settings.SelectionHighlightOverrideLineStyle = null;
    }

    public void Reset()
    {
        foreach (var bitmapImageDrawable in ImageModel)
        {
            bitmapImageDrawable.BitmapImage = null;
        }

        foreach (var bitmapImageROIDrawable in ROIModel)
        {
            bitmapImageROIDrawable.Rect = Rect.Empty;
            bitmapImageROIDrawable.IsFixed = false;
        }
    }
}
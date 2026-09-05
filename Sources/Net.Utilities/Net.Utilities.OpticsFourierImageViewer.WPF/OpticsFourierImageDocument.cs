using Net.Utilities.Graphics;
using Net.Utilities.Graphics.Interfaces;
using Net.Utilities.Graphics.Primitives.Medias.Layers;
using Net.Utilities.Graphics.Primitives.ObjectModels;
using Net.Utilities.OpticsFourierImageViewer.WPF.Drawables;

namespace Net.Utilities.OpticsFourierImageViewer.WPF;

public sealed class OpticsFourierImageDocument : CanvasDocument
{
    public readonly ILayer ROILayer = new ImmutableLayer("ROI", 200);
    public readonly ILayer ImageLayer = new ImmutableLayer("Image", 100);

    public Model<BitmapImageROIDrawable> ROIModel { get; }

    public Model<BitmapImageDrawable> ImageModel { get; }

    public OpticsFourierImageDocument()
    {
        ROIModel = ModelStorages.GetOrAddLayer<BitmapImageROIDrawable>(ROILayer);
        ImageModel = ModelStorages.GetOrAddLayer<BitmapImageDrawable>(ImageLayer);

        Settings.SelectionHighlightOverrideFillStyle = null;
        Settings.SelectionHighlightOverrideLineStyle = null;
    }
}
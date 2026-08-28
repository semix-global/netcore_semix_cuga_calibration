using Net.Utilities.Graphics;
using Net.Utilities.Graphics.Editors;
using Net.Utilities.Graphics.Interfaces;
using Net.Utilities.Graphics.Primitives.Medias.Layers;
using Net.Utilities.Graphics.Primitives.ObjectModels;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.Stage;

public sealed class StageMapDocument : CanvasDocument
{
    public readonly ILayer WaferLayer = new ImmutableLayer("Wafer", 200);
    public readonly ILayer DieLayer = new ImmutableLayer("Die", 100);

    public Model<StageMapWafer> WaferModel { get; }

    public Model<StageMapDie> DieModel { get; }

    public StageMapDocument()
    {
        WaferModel = ModelStorages.GetOrAddLayer<StageMapWafer>(WaferLayer);
        DieModel = ModelStorages.GetOrAddLayer<StageMapDie>(DieLayer);

        BackgroundEditor.GetOrAdd<StageMapSelectionBackgroundEditor>(Edit);
    }
}
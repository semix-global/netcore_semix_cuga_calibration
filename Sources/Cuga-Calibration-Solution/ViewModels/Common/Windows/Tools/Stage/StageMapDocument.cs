using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Graphics;
using Net.Utilities.Graphics.Editors;
using Net.Utilities.Graphics.Interfaces;
using Net.Utilities.Graphics.Primitives.EventArgs.Inputs;
using Net.Utilities.Graphics.Primitives.Medias.Layers;
using Net.Utilities.Graphics.Primitives.ObjectModels;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.Stage;

public sealed partial class StageMapDocument : CanvasDocument
{
    public readonly ILayer WaferLayer = new ImmutableLayer("Wafer", 100);
    public readonly ILayer DieLayer = new ImmutableLayer("Die", 200);

    [ObservableProperty]
    public partial string Position { get; set; } = string.Empty;

    public Model<StageMapWafer> WaferModel { get; }

    public Model<StageMapDie> DieModel { get; }

    public StageMapDocument()
    {
        WaferModel = ModelStorages.GetOrAddLayer<StageMapWafer>(WaferLayer);
        DieModel = ModelStorages.GetOrAddLayer<StageMapDie>(DieLayer);

        BackgroundEditor.GetOrAdd<StageMapBackgroundEditor>(Edit);
    }
}

public sealed class StageMapBackgroundEditor(CanvasEdit edit) : BackgroundEditor(edit)
{
    public override void OnCursorMove(CursorEventArgs e)
    {
        base.OnCursorMove(e);

        var canvasDocument = Guard.IsAssignableToTypeAndReturn<StageMapDocument>(Edit.Document);
        var result = $"0, 0 | {e.Point.ToString(canvasDocument.Settings.NumberFormat)}";

        var selectionPickDistance = canvasDocument.View.ScreenToWorldDistance(canvasDocument.Settings.SelectionPickDistance);

        foreach (var die in canvasDocument.DieModel)
        {
            if (die.Contains(e.Point, selectionPickDistance) == false) continue;

            result = $"{die.Index} | {e.Point.ToString(canvasDocument.Settings.NumberFormat)}";

            break;
        }

        canvasDocument.Position = result;
    }
}
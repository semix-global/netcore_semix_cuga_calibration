using CommunityToolkit.Diagnostics;
using Net.Utilities.Graphics;
using Net.Utilities.Graphics.Editors;
using Net.Utilities.Graphics.Primitives.Enums.Inputs;
using Net.Utilities.Graphics.Primitives.EventArgs.Inputs;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.Stage;

public sealed class StageMapSelectionBackgroundEditor(CanvasEdit edit) : BackgroundEditor(edit)
{
    public override void OnCursorClick(CursorEventArgs e) => Edit.Document.RunDesign(() =>
    {
        var stageMapDocument = Guard.IsAssignableToTypeAndReturn<StageMapDocument>(Edit.Document);
        
        if (e is not { CursorButtonEnum: CursorButtonEnum.Left }) return;
        
        foreach (var stageMapDie in Edit.SelectedItems) stageMapDie.IsSelected = false;
        Edit.SelectedItems.Clear();

        var selectionPickDistance = Edit.Document.View.ScreenToWorldDistance(Edit.Document.Settings.SelectionPickDistance);
        var item = stageMapDocument.DieModel
            .FirstOrDefault(d => d.Contains(e.Point, selectionPickDistance));

        if (item is null) return;

        item.IsSelected = true;
        Edit.SelectedItems.Add(item);
    });
}
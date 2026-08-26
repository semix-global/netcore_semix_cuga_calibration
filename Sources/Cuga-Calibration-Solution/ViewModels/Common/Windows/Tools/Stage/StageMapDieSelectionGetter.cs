using CommunityToolkit.Diagnostics;
using Net.Utilities.Graphics;
using Net.Utilities.Graphics.Editors;
using Net.Utilities.Graphics.Primitives.Editors.Getters;
using Net.Utilities.Graphics.Primitives.Editors.Getters.Options;
using Net.Utilities.Graphics.Primitives.ObjectModels;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.Stage;

public sealed class StageMapDieSelectionGetter(
    CanvasEdit edit,
    SelectionInputOptions<StageMapDie> options,
    TaskCompletionSource<OutputResult<SelectionSet<StageMapDie>>> completion) : GetterEditorSelectionSet<StageMapDie, SelectionInputOptions<StageMapDie>>(edit, options, completion)
{
    protected override void InitializeSelection(InitArgs<SelectionSet<StageMapDie>> initArgs)
    {
        Options.Initialize();
    }

    protected override SelectionSet<StageMapDie> GetSelectionFromWindow()
    {
        var document = Guard.IsAssignableToTypeAndReturn<StageMapDocument>(Edit.Document);

        Guard.IsNotNull(SelectionWindow);

        var results = new SelectionSet<StageMapDie>();

        var selectionWindowExtents = SelectionWindow.GetExtents();
        var visibleItems = (from item in document.DieModel
                where item.IsVisible
                let extents = item.GetExtents()
                select (extents, item))
            .ToArray();

        var selectedRows = visibleItems
            .Where(t => selectionWindowExtents.IntersectsWith(t.extents))
            .Select(t => t.item.Row)
            .ToHashSet();

        foreach (var item in visibleItems
                     .Where(t => selectedRows.Contains(t.item.Row))
                     .Select(t => t.item))
        {
            results.Add(item);
        }

        return results;
    }
}
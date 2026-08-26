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

        foreach (var item in from x in visibleItems
                 where SelectionWindow.IsPositiveSelection && selectionWindowExtents.Contains(x.extents) /* 正选 */
                       || SelectionWindow.IsPositiveSelection == false && selectionWindowExtents.IntersectsWith(x.extents) /* 反选 */
                 select x.item)
        {
            results.Add(item);
        }

        return results;
    }
}
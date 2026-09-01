using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Diagnostics;
using Net.Utilities.Graphics;
using Net.Utilities.Graphics.Drawables;
using Net.Utilities.Graphics.Editors;
using Net.Utilities.Graphics.Extensions;
using Net.Utilities.Graphics.Primitives.Editors;
using Net.Utilities.Graphics.Primitives.Editors.Getters;
using Net.Utilities.Graphics.Primitives.Enums.Inputs;
using Net.Utilities.Graphics.Primitives.EventArgs.Inputs;
using Net.Utilities.Models;
using Net.Utilities.Models.Geometries;
using Net.Utilities.OpticsFourierImageViewer.WPF.Drawables;
using Net.Utilities.OpticsFourierImageViewer.WPF.Extensions;
using Net.Utilities.OpticsFourierImageViewer.WPF.Primitives.Enums;

namespace Net.Utilities.OpticsFourierImageViewer.WPF.Editors;

public sealed class ModifyBitmapImageROIDrawableGetterEditor(
    CanvasEdit edit,
    ModifyBitmapImageROIDrawableInputOptions options,
    TaskCompletionSource<OutputResult<Unit>> completion) : GetterEditor<ModifyBitmapImageROIDrawableInputOptions, Unit>(edit, options, completion)
{
    private ImmutableArray<(BitmapImageROIDrawable BitmapImageROIDrawable, Rect OriginalRect, bool OriginalIsModified)> _originals = [];
    private ImmutableArray<(BitmapImageROIDrawable BitmapImageROIDrawable, Rect OriginalRect)> _edits = [];

    private CursorTypeEnum _lastCursorTypeEnum;
    private Point _lastMousePoint;
    private bool _isCursorDown;

    private SelectionWindow? _selectionWindow;

    private BitmapImageROIDrawableEditorStateEnum _editorStateEnum;
    private BitmapImageROIDrawableROIOperationModeEnum _roiOperationModeEnum;
    private BitmapImageROIResizeJoystickStateEnum _resizeJoystickStateEnum;

    private bool _isAccepted;

    protected override void Init(InitArgs<Unit> args)
    {
        base.Init(args);

        ClearSelection();
        ImmutableInterlocked.Update(ref _originals, _ => []);
        ResetInteractionState();

        if (Options.BitmapImageDrawable.BitmapImage?.IsEmpty != false)
        {
            args.ErrorInputMessage = "*Bitmap image is empty*";
            args.IsInputValid = false;
            args.IsContinueAsync = false;

            return;
        }

        using var scope = Edit.Document.View.Sync.EnterScope();
        foreach (var bitmapImageROIDrawable in Edit.Document.OverlayerModel.OfType<BitmapImageROIDrawable>()
                     .Where(t => ReferenceEquals(t.BitmapImageDrawable, Options.BitmapImageDrawable)))
        {
            bitmapImageROIDrawable.Rect = bitmapImageROIDrawable.Rect.ClampToBounds(Options.GetImageRect());
            ImmutableInterlocked.Update(ref _originals, t => t.Add((bitmapImageROIDrawable, bitmapImageROIDrawable.Rect, bitmapImageROIDrawable.IsModified)));
        }

        args.IsInputValid = true;
        args.IsContinueAsync = true;
    }

    protected override void CursorChanged(Point point)
    {
        DoPrompt(point.ToString(Edit.Document.Settings.NumberFormat));

        if (_isCursorDown == false) return;

        var currentMousePoint = point.ImageCoordinateRound();

        if (_selectionWindow is not null)
        {
            _selectionWindow.EndPoint = currentMousePoint;

            return;
        }

        if (_editorStateEnum == BitmapImageROIDrawableEditorStateEnum.Select || _edits.IsEmpty) return;

        ApplyModification(currentMousePoint - _lastMousePoint);
    }

    protected override void CursorDownInput(EventInputArgs<CursorEventArgs, Unit> eventInputArgs)
    {
        _lastCursorTypeEnum = Edit.Document.View.CanvasControl?.CursorTypeEnum ?? _lastCursorTypeEnum;

        if (eventInputArgs.CheckIsCursorButtonEnum(CursorButtonEnum.Left, CursorButtonStateEnum.Pressed) == false) return;

        var point = eventInputArgs.Event.Point.ImageCoordinateRound();

        _lastMousePoint = point;
        _isCursorDown = true;

        Guard.IsTrue(_edits.IsEmpty);

        Guard.IsTrue(_editorStateEnum == BitmapImageROIDrawableEditorStateEnum.Select);
        Guard.IsTrue(_roiOperationModeEnum == BitmapImageROIDrawableROIOperationModeEnum.None);
        Guard.IsTrue(_resizeJoystickStateEnum == BitmapImageROIResizeJoystickStateEnum.None);

        var isControlPressed = eventInputArgs.Event.ModifierKeysEnum.IsPressed(ModifierKeysEnum.Control);
        if (TryGetModifyTarget(out var target, out var controlPoint))
        {
            if (isControlPressed)
            {
                ToggleSelection(target);

                _isCursorDown = false;
            }
            else if (controlPoint is null && Options.BitmapImageROIDragMoveTypeEnum == BitmapImageROIDragMoveTypeEnum.None) // 不支持拖拽移动
            {
                _isCursorDown = false;
            }
            else
            {
                _editorStateEnum = BitmapImageROIDrawableEditorStateEnum.Modify;
                foreach (var rectROIDrawable in Edit.SelectedItems.OfType<BitmapImageROIDrawable>())
                {
                    ImmutableInterlocked.Update(ref _edits, t => t.Add((rectROIDrawable, rectROIDrawable.Rect)));
                }

                if (controlPoint is not null)
                {
                    _roiOperationModeEnum = BitmapImageROIDrawableROIOperationModeEnum.Resize;
                    (_resizeJoystickStateEnum, var cursorTypeEnum) = controlPoint.GetControlPointInformation();

                    Edit.Document.View.CanvasControl?.CursorTypeEnum = cursorTypeEnum;
                }
                else
                {
                    _roiOperationModeEnum = BitmapImageROIDrawableROIOperationModeEnum.Move;
                    _resizeJoystickStateEnum = BitmapImageROIResizeJoystickStateEnum.None;

                    Edit.Document.View.CanvasControl?.CursorTypeEnum = Options.BitmapImageROIDragMoveTypeEnum.GetMoveCursorTypeEnum();
                }
            }
        }
        else BeginSelection(point, isControlPressed);

        eventInputArgs.IsInputValid = true;
        eventInputArgs.IsInputCompleted = false;

        return;

        bool TryGetModifyTarget([NotNullWhen(true)] out BitmapImageROIDrawable? selectedBitmapImageROIDrawable, out ControlPoint? selectedControlPoint)
        {
            using var scope = Edit.Document.View.Sync.EnterScope();

            selectedBitmapImageROIDrawable = null;
            selectedControlPoint = null;

            var selectedRectROIDrawables = Edit.SelectedItems.OfType<BitmapImageROIDrawable>().ToArray();
            if (selectedRectROIDrawables.Length == 0) return false;

            var controlPointPickDistance = Edit.Document.View.ScreenToWorldDistance(Edit.Document.Settings.ControlPointPickDistance);

            foreach (var rectROIDrawable in selectedRectROIDrawables)
            {
                selectedControlPoint = rectROIDrawable.GetControlPoints()
                    .FirstOrDefault(t => (t.BasePoint - point).Length <= controlPointPickDistance);

                if (selectedControlPoint is null) continue;

                selectedBitmapImageROIDrawable = rectROIDrawable;

                return true;
            }

            foreach (var rectROIDrawable in selectedRectROIDrawables)
            {
                if (rectROIDrawable.Contains(point, controlPointPickDistance) == false) continue;

                selectedBitmapImageROIDrawable = rectROIDrawable;

                return true;
            }

            return false;
        }
    }

    protected override void CursorUpInput(EventInputArgs<CursorEventArgs, Unit> eventInputArgs)
    {
        if (_selectionWindow is not null) CompleteSelection(eventInputArgs.Event.ModifierKeysEnum.IsPressed(ModifierKeysEnum.Control));

        Edit.Document.View.CanvasControl?.CursorTypeEnum = _lastCursorTypeEnum;

        ResetInteractionState();

        eventInputArgs.IsInputValid = true;
        eventInputArgs.IsInputCompleted = false;
    }

    protected override void KeyDownInput(EventInputArgs<KeyEventArgs, Unit> eventInputArgs)
    {
        switch (eventInputArgs.Event.KeyEnum)
        {
            case KeyEnum.Enter:
                _isAccepted = true;

                eventInputArgs.Output = Unit.Default;

                eventInputArgs.IsInputValid = true;
                eventInputArgs.IsInputCompleted = true;

                return;

            case KeyEnum.Escape:
                _isAccepted = false;

                eventInputArgs.ErrorInputMessage = "Escape";
                eventInputArgs.Output = Unit.Default;

                eventInputArgs.IsInputValid = false;
                eventInputArgs.IsInputCompleted = true;

                return;

            default:
                eventInputArgs.IsInputValid = true;
                eventInputArgs.IsInputCompleted = false;

                break;
        }
    }

    protected override void CancelInput()
    {
        if (_isAccepted == false)
        {
            foreach (var (rectROIDrawable, originalRect, originalIsModified) in _originals)
            {
                rectROIDrawable.Rect = originalRect;
                rectROIDrawable.IsModified = originalIsModified;
            }
        }

        ClearSelection();
        ImmutableInterlocked.Update(ref _originals, _ => []);
        ResetInteractionState();
    }

    private void ResetInteractionState()
    {
        RemoveSelectionWindow();

        ImmutableInterlocked.Update(ref _edits, _ => []);

        _lastCursorTypeEnum = CursorTypeEnum.Arrow;
        _lastMousePoint = Point.Origin;
        _isCursorDown = false;

        _selectionWindow = null;

        _editorStateEnum = BitmapImageROIDrawableEditorStateEnum.Select;
        _roiOperationModeEnum = BitmapImageROIDrawableROIOperationModeEnum.None;
        _resizeJoystickStateEnum = BitmapImageROIResizeJoystickStateEnum.None;

        _isAccepted = false;
    }

    #region 选择编辑的ROI

    private void BeginSelection(Point point, bool isControlPressed)
    {
        using var scope = Edit.Document.View.Sync.EnterScope();

        _editorStateEnum = BitmapImageROIDrawableEditorStateEnum.Select;

        if (isControlPressed == false) ClearSelection();

        RemoveSelectionWindow();

        _selectionWindow = new SelectionWindow(point, point);

        Edit.Document.Transients.Add(_selectionWindow);
    }

    private void CompleteSelection(bool isControlPressed)
    {
        using var scope = Edit.Document.View.Sync.EnterScope();

        Guard.IsNotNull(_selectionWindow);

        var selectionWindow = _selectionWindow;
        RemoveSelectionWindow();

        BitmapImageROIDrawable[] selectedItems =
        [
            .. Edit.Document.View.VisibleItems
                .OfType<BitmapImageROIDrawable>()
                .Where(t => selectionWindow.GetExtents().IntersectsWith(t.GetExtents()))
        ];

        if (isControlPressed == false) ClearSelection();

        foreach (var rectROIDrawable in selectedItems)
        {
            if (isControlPressed) ToggleSelection(rectROIDrawable);
            else Select(rectROIDrawable);
        }
    }

    private void ToggleSelection(BitmapImageROIDrawable bitmapImageROIDrawable)
    {
        using var scope = Edit.Document.View.Sync.EnterScope();

        if (Edit.SelectedItems.Contains(bitmapImageROIDrawable))
        {
            bitmapImageROIDrawable.IsSelected = false;

            Edit.SelectedItems.Remove(bitmapImageROIDrawable);
        }
        else Select(bitmapImageROIDrawable);
    }

    private void Select(BitmapImageROIDrawable bitmapImageROIDrawable)
    {
        using var scope = Edit.Document.View.Sync.EnterScope();

        if (Edit.SelectedItems.Contains(bitmapImageROIDrawable)) return;

        bitmapImageROIDrawable.IsSelected = true;
        Edit.SelectedItems.Add(bitmapImageROIDrawable);
    }

    private void RemoveSelectionWindow()
    {
        using var scope = Edit.Document.View.Sync.EnterScope();

        if (_selectionWindow is null) return;

        Edit.Document.Transients.Remove(_selectionWindow);
        _selectionWindow = null;
    }

    private void ClearSelection()
    {
        using var scope = Edit.Document.View.Sync.EnterScope();

        foreach (var item in Edit.SelectedItems) item.IsSelected = false;

        Edit.SelectedItems.Clear();
    }

    #endregion

    #region 修改

    private void ApplyModification(Vector delta)
    {
        if (_edits.IsEmpty) return;

        switch (_roiOperationModeEnum)
        {
            case BitmapImageROIDrawableROIOperationModeEnum.Move:
                ApplyMove(delta);

                break;

            case BitmapImageROIDrawableROIOperationModeEnum.Resize:
                ApplyResize(delta);

                break;

            case BitmapImageROIDrawableROIOperationModeEnum.None:
            default:
                ThrowHelper.ThrowArgumentOutOfRangeException<Rect>(nameof(_roiOperationModeEnum));

                break;
        }
    }

    private void ApplyMove(Vector delta)
    {
        var imageRect = Options.GetImageRect();

        var dragMoveTypeEnum = Options.BitmapImageROIDragMoveTypeEnum;

        // 根据拖拽移动类型, 决定是否允许在 X 或 Y 方向移动
        delta = delta
            .WithX((dragMoveTypeEnum & BitmapImageROIDragMoveTypeEnum.X) == BitmapImageROIDragMoveTypeEnum.X ? delta.X : 0)
            .WithY((dragMoveTypeEnum & BitmapImageROIDragMoveTypeEnum.Y) == BitmapImageROIDragMoveTypeEnum.Y ? delta.Y : 0);

        // 强制移动的范围: 用全部选中ROI的外接范围计算共同位移边界, 避免任何对象越过图片边缘
        var minX = _edits.Min(t => t.OriginalRect.XMin);
        var maxX = _edits.Max(t => t.OriginalRect.XMax);
        var minY = _edits.Min(t => t.OriginalRect.YMin);
        var maxY = _edits.Max(t => t.OriginalRect.YMax);

        var constrainedDelta = new Vector(
            Math.Clamp(delta.X, imageRect.XMin - minX, imageRect.XMax - maxX),
            Math.Clamp(delta.Y, imageRect.YMin - minY, imageRect.YMax - maxY));

        foreach (var (bitmapImageROIDrawable, originalRect) in _edits)
        {
            bitmapImageROIDrawable.Rect = (originalRect + constrainedDelta).ClampToBounds(imageRect);
        }
    }

    private void ApplyResize(Vector delta)
    {
        var imageRect = Options.GetImageRect();

        // 多选缩放使用相同锚点和拖动向量, 但每个 ROI 独立限制在图片内
        foreach (var (bitmapImageROIDrawable, originalRect) in _edits)
        {
            var modifiedRect = _resizeJoystickStateEnum switch
            {
                BitmapImageROIResizeJoystickStateEnum.None => originalRect,
                BitmapImageROIResizeJoystickStateEnum.XMinYMax => (Rect)new Extents(
                    new Point(originalRect.XMin, originalRect.YMax) + delta,
                    new Point(originalRect.XMax, originalRect.YMin)),
                BitmapImageROIResizeJoystickStateEnum.XCenterYMax => (Rect)new Extents(
                    new Point(originalRect.XMax, originalRect.YMax) + new Vector(0, delta.Y),
                    new Point(originalRect.XMin, originalRect.YMin)),
                BitmapImageROIResizeJoystickStateEnum.XMaxYMax => (Rect)new Extents(
                    new Point(originalRect.XMax, originalRect.YMax) + delta,
                    new Point(originalRect.XMin, originalRect.YMin)),
                BitmapImageROIResizeJoystickStateEnum.XMaxYCenter => (Rect)new Extents(
                    new Point(originalRect.XMax, originalRect.YMax) + new Vector(delta.X, 0),
                    new Point(originalRect.XMin, originalRect.YMin)),
                BitmapImageROIResizeJoystickStateEnum.XMaxYMin => (Rect)new Extents(
                    new Point(originalRect.XMax, originalRect.YMin) + delta,
                    new Point(originalRect.XMin, originalRect.YMax)),
                BitmapImageROIResizeJoystickStateEnum.XCenterYMin => (Rect)new Extents(
                    new Point(originalRect.XMin, originalRect.YMin) + new Vector(0, delta.Y),
                    new Point(originalRect.XMax, originalRect.YMax)),
                BitmapImageROIResizeJoystickStateEnum.XMinYMin => (Rect)new Extents(
                    new Point(originalRect.XMin, originalRect.YMin) + delta,
                    new Point(originalRect.XMax, originalRect.YMax)),
                BitmapImageROIResizeJoystickStateEnum.XMinYCenter => (Rect)new Extents(
                    new Point(originalRect.XMin, originalRect.YMin) + new Vector(delta.X, 0),
                    new Point(originalRect.XMax, originalRect.YMax)),
                _ => ThrowHelper.ThrowArgumentOutOfRangeException<Rect>(nameof(_resizeJoystickStateEnum))
            };

            bitmapImageROIDrawable.Rect = modifiedRect.ClampToBounds(imageRect);
        }
    }

    #endregion
}
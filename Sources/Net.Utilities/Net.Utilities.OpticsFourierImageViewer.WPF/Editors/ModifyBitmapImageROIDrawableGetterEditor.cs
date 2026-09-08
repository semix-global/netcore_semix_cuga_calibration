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
    private ImmutableDictionary<BitmapImageROIDrawable, Rect> _originalDictionary = [];
    private ImmutableArray<(BitmapImageROIDrawable BitmapImageROIDrawable, Rect OriginalRect)> _edits = [];
    private ImmutableStack<ImmutableArray<(BitmapImageROIDrawable BitmapImageROIDrawable, Rect OriginalRect, Rect ModifiedRect)>> _undoStack = [];
    private ImmutableStack<ImmutableArray<(BitmapImageROIDrawable BitmapImageROIDrawable, Rect OriginalRect, Rect ModifiedRect)>> _redoStack = [];

    private CursorTypeEnum? _lastCursorTypeEnum;
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

        _lastCursorTypeEnum = null;

        Reset();

        if (Options.BitmapImageDrawable.BitmapImage?.IsEmpty != false)
        {
            args.ErrorInputMessage = "*Bitmap image is empty*";
            args.IsInputValid = false;
            args.IsContinueAsync = false;

            return;
        }

        var opticsFourierImageDocument = Guard.IsAssignableToTypeAndReturn<OpticsFourierImageDocument>(Edit.Document);

        using var scope = opticsFourierImageDocument.View.Sync.EnterScope();

        var builder = ImmutableDictionary.CreateBuilder<BitmapImageROIDrawable, Rect>();

        foreach (var bitmapImageROIDrawable in opticsFourierImageDocument.ROIModel
                     .Where(t => ReferenceEquals(t.BitmapImageDrawable, Options.BitmapImageDrawable) && t.Layer.IsVisible && t is { IsVisible: true, IsFixed: false }))
        {
            bitmapImageROIDrawable.Rect = bitmapImageROIDrawable.Rect.ImageCoordinateRound().ClampToBounds(Options.GetImageRect());
            bitmapImageROIDrawable.IsEditorModified = false;
            builder.Add(bitmapImageROIDrawable, bitmapImageROIDrawable.Rect);
        }

        _originalDictionary = builder.ToImmutable();

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
        _lastCursorTypeEnum = Edit.Document.View.CanvasControl?.CursorTypeEnum;

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

                _edits =
                [
                    .. Edit.SelectedItems.OfType<BitmapImageROIDrawable>()
                        .Where(t => ReferenceEquals(t.BitmapImageDrawable, Options.BitmapImageDrawable) && t.Layer.IsVisible && t is { IsVisible: true, IsFixed: false })
                        .Select(t => (t, t.Rect))
                ];

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

            var selectedRectROIDrawables = Edit.SelectedItems.OfType<BitmapImageROIDrawable>()
                .Where(t => ReferenceEquals(t.BitmapImageDrawable, Options.BitmapImageDrawable) && t.Layer.IsVisible && t is { IsVisible: true, IsFixed: false })
                .ToArray();
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
        if (eventInputArgs.CheckIsCursorButtonEnum(CursorButtonEnum.Left, CursorButtonStateEnum.Released) == false) return;

        if (_selectionWindow is not null)
        {
            _selectionWindow.EndPoint = eventInputArgs.Event.Point.ImageCoordinateRound();
            CompleteSelection(eventInputArgs.Event.ModifierKeysEnum.IsPressed(ModifierKeysEnum.Control));
        }

        CommitToHistory();
        ResetInteractionState();

        eventInputArgs.IsInputValid = true;
        eventInputArgs.IsInputCompleted = false;
    }

    protected override void KeyDownInput(EventInputArgs<KeyEventArgs, Unit> eventInputArgs)
    {
        if (eventInputArgs.Event.KeyEnum == KeyEnum.Z && eventInputArgs.Event.ModifierKeysEnum.IsPressed(ModifierKeysEnum.Control))
        {
            CommitToHistory();
            if (_edits.IsEmpty == false) ResetInteractionState();

            if (eventInputArgs.Event.ModifierKeysEnum.IsPressed(ModifierKeysEnum.Shift)) Redo();
            else Undo();

            eventInputArgs.IsInputValid = true;
            eventInputArgs.IsInputCompleted = false;

            return;
        }

        if (eventInputArgs.Event.KeyEnum == KeyEnum.Enter)
        {
            _isAccepted = true;

            eventInputArgs.Output = Unit.Default;

            eventInputArgs.IsInputValid = true;
            eventInputArgs.IsInputCompleted = true;

            return;
        }

        eventInputArgs.IsInputValid = true;
        eventInputArgs.IsInputCompleted = false;
    }

    protected override void CancelInput()
    {
        if (_isAccepted == false)
        {
            foreach (var (rectROIDrawable, originalRect) in _originalDictionary)
            {
                rectROIDrawable.Rect = originalRect;
                rectROIDrawable.IsEditorModified = false;
            }
        }

        Reset();
    }

    private void Reset()
    {
        ClearSelection();
        _originalDictionary = [];
        _undoStack = [];
        _redoStack = [];

        ResetInteractionState();
    }

    private void ResetInteractionState()
    {
        if (_lastCursorTypeEnum is not null) Edit.Document.View.CanvasControl?.CursorTypeEnum = _lastCursorTypeEnum.Value;

        RemoveSelectionWindow();

        _edits = [];

        _lastCursorTypeEnum = null;
        _lastMousePoint = Point.Origin;
        _isCursorDown = false;

        _selectionWindow = null;

        _editorStateEnum = BitmapImageROIDrawableEditorStateEnum.Select;
        _roiOperationModeEnum = BitmapImageROIDrawableROIOperationModeEnum.None;
        _resizeJoystickStateEnum = BitmapImageROIResizeJoystickStateEnum.None;

        _isAccepted = false;
    }

    #region 历史记录

    private void CommitToHistory()
    {
        if (_edits.IsEmpty) return;

        var edit = _edits
            .Where(t => t.OriginalRect != t.BitmapImageROIDrawable.Rect)
            .Select(t => (
                t.BitmapImageROIDrawable,
                t.OriginalRect,
                ModifiedRect: t.BitmapImageROIDrawable.Rect))
            .ToImmutableArray();

        if (edit.IsEmpty) return;

        _undoStack = _undoStack.Push(edit);
        _redoStack = [];
    }

    private void Undo()
    {
        if (_undoStack.IsEmpty) return;

        _undoStack = _undoStack.Pop(out var edit);

        ApplyHistory(edit, true);

        _redoStack = _redoStack.Push(edit);
    }

    private void Redo()
    {
        if (_redoStack.IsEmpty) return;

        _redoStack = _redoStack.Pop(out var edit);

        ApplyHistory(edit, false);

        _undoStack = _undoStack.Push(edit);
    }

    private void ApplyHistory(ImmutableArray<(BitmapImageROIDrawable BitmapImageROIDrawable, Rect OriginalRect, Rect ModifiedRect)> edit, bool isUndo)
    {
        foreach (var item in edit)
        {
            item.BitmapImageROIDrawable.Rect = isUndo
                ? item.OriginalRect
                : item.ModifiedRect;

            item.BitmapImageROIDrawable.IsEditorModified = _originalDictionary[item.BitmapImageROIDrawable] != item.BitmapImageROIDrawable.Rect;
        }
    }

    #endregion

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
        var opticsFourierImageDocument = Guard.IsAssignableToTypeAndReturn<OpticsFourierImageDocument>(Edit.Document);

        using var scope = opticsFourierImageDocument.View.Sync.EnterScope();

        Guard.IsNotNull(_selectionWindow);

        var selectionWindow = _selectionWindow;
        RemoveSelectionWindow();

        BitmapImageROIDrawable[] selectedItems =
        [
            .. opticsFourierImageDocument.ROIModel
                .Where(t => ReferenceEquals(t.BitmapImageDrawable, Options.BitmapImageDrawable) && t.Layer.IsVisible && t is { IsVisible: true, IsFixed: false })
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

        if (bitmapImageROIDrawable.IsFixed) return;

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

        if (bitmapImageROIDrawable.IsFixed) return;

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
            bitmapImageROIDrawable.IsEditorModified = _originalDictionary[bitmapImageROIDrawable] != bitmapImageROIDrawable.Rect;
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
            bitmapImageROIDrawable.IsEditorModified = _originalDictionary[bitmapImageROIDrawable] != bitmapImageROIDrawable.Rect;
        }
    }

    #endregion
}
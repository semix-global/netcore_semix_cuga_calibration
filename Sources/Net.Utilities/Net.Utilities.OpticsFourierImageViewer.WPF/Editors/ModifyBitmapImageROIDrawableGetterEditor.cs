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
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Net.Utilities.OpticsFourierImageViewer.WPF.Editors;

public sealed class ModifyBitmapImageROIDrawableGetterEditor(
    CanvasEdit edit,
    ModifyBitmapImageROIDrawableInputOptions options,
    TaskCompletionSource<OutputResult<Unit>> completion) : GetterEditor<ModifyBitmapImageROIDrawableInputOptions, Unit>(edit, options, completion)
{
    private ImmutableDictionary<BitmapImageROIDrawable, ROIState> _originalDictionary = [];
    private ImmutableArray<(BitmapImageROIDrawable BitmapImageROIDrawable, ROIState OriginalState)> _edits = [];
    private ImmutableStack<ImmutableArray<(BitmapImageROIDrawable BitmapImageROIDrawable, ROIState OriginalState, ROIState ModifiedState)>> _undoStack = [];
    private ImmutableStack<ImmutableArray<(BitmapImageROIDrawable BitmapImageROIDrawable, ROIState OriginalState, ROIState ModifiedState)>> _redoStack = [];

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
        using var scope = Edit.Document.View.Sync.EnterScope();

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

        var builder = ImmutableDictionary.CreateBuilder<BitmapImageROIDrawable, ROIState>();

        foreach (var bitmapImageROIDrawable in opticsFourierImageDocument.ROIModel
                     .Where(t => ReferenceEquals(t.BitmapImageDrawable, Options.BitmapImageDrawable) && t.Layer.IsVisible && t is { IsVisible: true, IsFixed: false }))
        {
            bitmapImageROIDrawable.Rect = bitmapImageROIDrawable.Rect.ImageCoordinateRound().ClampToBounds(Options.GetImageRect());
            bitmapImageROIDrawable.IsEditorModified = false;
            builder.Add(bitmapImageROIDrawable, ROIState.From(bitmapImageROIDrawable));
        }

        _originalDictionary = builder.ToImmutable();

        args.IsInputValid = true;
        args.IsContinueAsync = true;
    }

    protected override void CursorChanged(Point point)
    {
        using var scope = Edit.Document.View.Sync.EnterScope();

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
        using var scope = Edit.Document.View.Sync.EnterScope();

        if (eventInputArgs.CheckIsCursorButtonEnum(CursorButtonEnum.Left, CursorButtonStateEnum.Pressed) == false) return;

        _lastCursorTypeEnum = Edit.Document.View.CanvasControl?.CursorTypeEnum;

        var point = eventInputArgs.Event.Point.ImageCoordinateRound();

        _lastMousePoint = point;
        _isCursorDown = true;

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

                _edits = GetEdits(); // Delete 中的赋值只在Delete执行, 而且结束后, ResetInteractionState() 会清空 _edits.

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
        using var scope = Edit.Document.View.Sync.EnterScope();

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
        using var scope = Edit.Document.View.Sync.EnterScope();

        if (eventInputArgs.Event.KeyEnum == KeyEnum.Z && eventInputArgs.Event.ModifierKeysEnum.IsPressed(ModifierKeysEnum.Control))
        {
            CommitToHistory();
            ResetInteractionState();

            if (eventInputArgs.Event.ModifierKeysEnum.IsPressed(ModifierKeysEnum.Shift)) Redo();
            else Undo();

            eventInputArgs.IsInputValid = true;
            eventInputArgs.IsInputCompleted = false;

            return;
        }

        if (eventInputArgs.Event.KeyEnum == KeyEnum.Delete && Options.IsDeleteEnabled)
        {
            DeleteSelectedROIs();

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
        using var scope = Edit.Document.View.Sync.EnterScope();

        if (_isAccepted == false)
        {
            foreach (var (rectROIDrawable, originalState) in _originalDictionary) ApplyROIState(rectROIDrawable, originalState);
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

        _isAccepted = false;
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
    }

    #region 历史记录

    private void CommitToHistory()
    {
        if (_edits.IsEmpty) return;

        var edit = _edits
            .Where(t => t.OriginalState != ROIState.From(t.BitmapImageROIDrawable))
            .Select(t => (
                t.BitmapImageROIDrawable,
                t.OriginalState,
                ModifiedState: ROIState.From(t.BitmapImageROIDrawable)))
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

    private void ApplyHistory(ImmutableArray<(BitmapImageROIDrawable BitmapImageROIDrawable, ROIState OriginalState, ROIState ModifiedState)> edit, bool isUndo)
    {
        foreach (var item in edit)
        {
            ApplyROIState(item.BitmapImageROIDrawable, isUndo
                ? item.OriginalState
                : item.ModifiedState);
        }
    }

    #endregion

    #region 选择编辑的ROI

    private void BeginSelection(Point point, bool isControlPressed)
    {
        _editorStateEnum = BitmapImageROIDrawableEditorStateEnum.Select;

        if (isControlPressed == false) ClearSelection();

        RemoveSelectionWindow();

        _selectionWindow = new SelectionWindow(point, point);

        Edit.Document.Transients.Add(_selectionWindow);
    }

    private void CompleteSelection(bool isControlPressed)
    {
        var opticsFourierImageDocument = Guard.IsAssignableToTypeAndReturn<OpticsFourierImageDocument>(Edit.Document);

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
        if (bitmapImageROIDrawable.IsFixed) return;

        if (Edit.SelectedItems.Contains(bitmapImageROIDrawable)) return;

        bitmapImageROIDrawable.IsSelected = true;
        Edit.SelectedItems.Add(bitmapImageROIDrawable);
    }

    private void RemoveSelectionWindow()
    {
        if (_selectionWindow is null) return;

        Edit.Document.Transients.Remove(_selectionWindow);
        _selectionWindow = null;
    }

    private void ClearSelection()
    {
        foreach (var item in Edit.SelectedItems) item.IsSelected = false;

        Edit.SelectedItems.Clear();
    }

    #endregion

    #region 修改

    private ImmutableArray<(BitmapImageROIDrawable BitmapImageROIDrawable, ROIState OriginalState)> GetEdits()
    {
        return
        [
            .. Edit.SelectedItems.OfType<BitmapImageROIDrawable>()
                .Where(t => ReferenceEquals(t.BitmapImageDrawable, Options.BitmapImageDrawable) && t.Layer.IsVisible && t is { IsVisible: true, IsFixed: false })
                .Where(t => _originalDictionary.ContainsKey(t)) // 保证 _edits ⊆ _originalDictionary 的键, 防止 Init 后新增/状态变更的 ROI 在索引查找时抛 KeyNotFoundException
                .Select(t => (t, ROIState.From(t)))
        ];
    }

    private void ApplyROIState(BitmapImageROIDrawable bitmapImageROIDrawable, ROIState state)
    {
        bitmapImageROIDrawable.Rect = state.Rect;
        bitmapImageROIDrawable.IsVisible = state.IsVisible;

        if (state.IsVisible == false)
        {
            bitmapImageROIDrawable.IsSelected = false;
            Edit.SelectedItems.Remove(bitmapImageROIDrawable);
        }

        bitmapImageROIDrawable.IsEditorModified = _originalDictionary[bitmapImageROIDrawable] != ROIState.From(bitmapImageROIDrawable);
    }

    private void DeleteSelectedROIs()
    {
        // 先提交当前拖拽, 使位置修改和批量隐藏分别作为一次历史操作.
        CommitToHistory();
        ResetInteractionState();

        try
        {
            _edits = GetEdits(); // CursorDownInput 中的赋值只在开始拖拽或缩放时执行, 而且鼠标松开后, ResetInteractionState() 会清空 _edits.

            foreach (var (bitmapImageROIDrawable, originalState) in _edits) ApplyROIState(bitmapImageROIDrawable, originalState with { IsVisible = false });
        }
        finally
        {
            CommitToHistory();
            ResetInteractionState();
        }
    }

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
        var minX = _edits.Min(t => t.OriginalState.Rect.XMin);
        var maxX = _edits.Max(t => t.OriginalState.Rect.XMax);
        var minY = _edits.Min(t => t.OriginalState.Rect.YMin);
        var maxY = _edits.Max(t => t.OriginalState.Rect.YMax);

        var constrainedDelta = new Vector(
            Math.Clamp(delta.X, imageRect.XMin - minX, imageRect.XMax - maxX),
            Math.Clamp(delta.Y, imageRect.YMin - minY, imageRect.YMax - maxY));

        foreach (var (bitmapImageROIDrawable, originalState) in _edits)
        {
            bitmapImageROIDrawable.Rect = (originalState.Rect + constrainedDelta).ClampToBounds(imageRect);
            bitmapImageROIDrawable.IsEditorModified = _originalDictionary[bitmapImageROIDrawable] != ROIState.From(bitmapImageROIDrawable);
        }
    }

    private void ApplyResize(Vector delta)
    {
        var imageRect = Options.GetImageRect();

        // 多选缩放使用相同锚点和拖动向量, 但每个 ROI 独立限制在图片内
        foreach (var (bitmapImageROIDrawable, originalState) in _edits)
        {
            var originalRect = originalState.Rect;
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
            bitmapImageROIDrawable.IsEditorModified = _originalDictionary[bitmapImageROIDrawable] != ROIState.From(bitmapImageROIDrawable);
        }
    }

    #endregion

    private readonly record struct ROIState(Rect Rect, bool IsVisible)
    {
        public static ROIState From(BitmapImageROIDrawable bitmapImageROIDrawable) => new(bitmapImageROIDrawable.Rect, bitmapImageROIDrawable.IsVisible);
    }
}
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

namespace Net.Utilities.OpticsFourierImageViewer.WPF.Editors;

public sealed class ModifyBitmapImageROIDrawableGetterEditor(
    CanvasEdit edit,
    ModifyBitmapImageROIDrawableInputOptions options,
    TaskCompletionSource<OutputResult<Unit>> completion) : GetterEditor<ModifyBitmapImageROIDrawableInputOptions, Unit>(edit, options, completion)
{
    // 取消、超时或外部取消时使用快照恢复；Enter 成功结束时保留当前修改。
    private readonly Dictionary<BitmapImageROIDrawable, (Rect Rect, bool IsModified)> _originalROIStates = [];

    // 每次鼠标拖拽都从同一份原始矩形计算，保证多选对象使用相同位移或缩放向量。
    private readonly Dictionary<BitmapImageROIDrawable, Rect> _dragOriginalRects = [];

    private SelectionWindow? _selectionWindow;
    private BitmapImageROIDrawable? _editRectROIDrawable;
    private Point _dragStartPoint;
    private CursorTypeEnum _defaultCursorTypeEnum;
    private EditorStateEnum _editorStateEnum;
    private ROIOperationModeEnum _roiOperationModeEnum;
    private ResizeJoystickStateEnum _resizeJoystickStateEnum;
    private bool _isCursorDown;
    private bool _isToggleSelection;
    private bool _isAccepted;

    protected override void Init(InitArgs<Unit> args)
    {
        base.Init(args);

        // 编辑器只选择和修改已有 ROI，不创建新的 RectROIDrawable。
        _editorStateEnum = EditorStateEnum.Select;
        _isCursorDown = false;
        _isToggleSelection = false;
        _isAccepted = false;
        _editRectROIDrawable = null;
        _dragOriginalRects.Clear();
        _originalROIStates.Clear();
        _roiOperationModeEnum = ROIOperationModeEnum.None;
        _resizeJoystickStateEnum = ResizeJoystickStateEnum.None;

        ClearSelection();

        if (Options.BitmapImageDrawable.BitmapImage?.IsEmpty != false)
        {
            args.ErrorInputMessage = "*Bitmap image is empty*";
            args.IsInputValid = false;
            args.IsContinueAsync = false;

            return;
        }

        var imageRect = GetImageRect();
        foreach (var rectROIDrawable in GetRectROIDrawables())
        {
            // 进入编辑器时先修正已有数据，保证后续快照本身就在图片范围内。
            rectROIDrawable.Rect = ClampRectToImage(rectROIDrawable.Rect, imageRect);
            _originalROIStates[rectROIDrawable] = (rectROIDrawable.Rect, rectROIDrawable.IsModified);
        }

        args.IsInputValid = true;
        args.IsContinueAsync = true;
    }

    protected override void CursorChanged(Point point)
    {
        DoPrompt(point.ToString(Edit.Document.Settings.NumberFormat));

        if (_isCursorDown == false) return;

        var currentPoint = point.ImageCoordinateRound();

        if (_selectionWindow is not null)
        {
            // 框选期间只更新临时窗口，不修改 ROI。
            Edit.Document.RunDesign(() => _selectionWindow.EndPoint = currentPoint);

            return;
        }

        if (_editorStateEnum != EditorStateEnum.Modify || _editRectROIDrawable is null) return;

        Edit.Document.RunDesign(() => ApplyModification(currentPoint));
    }

    protected override void CursorDownInput(EventInputArgs<CursorEventArgs, Unit> eventInputArgs)
    {
        _defaultCursorTypeEnum = Edit.Document.View.CanvasControl?.CursorTypeEnum ?? CursorTypeEnum.Arrow;

        if (eventInputArgs.CheckIsCursorButtonEnum(CursorButtonEnum.Left, CursorButtonStateEnum.Pressed) == false) return;

        var point = eventInputArgs.Event.Point.ImageCoordinateRound();

        _isCursorDown = true;
        _dragStartPoint = point;
        _editRectROIDrawable = null;
        _dragOriginalRects.Clear();
        _roiOperationModeEnum = ROIOperationModeEnum.None;
        _resizeJoystickStateEnum = ResizeJoystickStateEnum.None;

        var isControlPressed = eventInputArgs.Event.ModifierKeysEnum.IsPressed(ModifierKeysEnum.Control);

        var hasModifyTarget = TryGetModifyTarget(point, out var target, out var controlPoint);

        if (hasModifyTarget)
        {
            Guard.IsNotNull(target);

            // Ctrl 点击已选 ROI 时切换其选中状态；否则继续进行普通修改流程。
            if (isControlPressed)
            {
                ToggleSelection(target);
                _isCursorDown = false;
            }
            else if (controlPoint is null && Options.IsEnableDragMove == false)
            {
                // 禁止移动时保留当前选择，但不创建拖拽操作。
                _isCursorDown = false;
            }
            else
            {
                BeginModification(target, controlPoint, point);
            }
        }
        else
        {
            BeginSelection(point, isControlPressed);
        }

        eventInputArgs.IsInputValid = true;
        eventInputArgs.IsInputCompleted = false;
    }

    protected override void CursorUpInput(EventInputArgs<CursorEventArgs, Unit> eventInputArgs)
    {
        if (eventInputArgs.CheckIsCursorButtonEnum(CursorButtonEnum.Left, CursorButtonStateEnum.Released) == false) return;

        var point = eventInputArgs.Event.Point.ImageCoordinateRound();

        if (_selectionWindow is not null)
        {
            Edit.Document.RunDesign(() => _selectionWindow.EndPoint = point);
            CompleteSelection();
        }

        ResetInteractionState();

        eventInputArgs.IsInputValid = true;
        eventInputArgs.IsInputCompleted = false;
    }

    protected override void KeyDownInput(EventInputArgs<KeyEventArgs, Unit> eventInputArgs)
    {
        if (eventInputArgs.Event.KeyEnum == KeyEnum.Enter)
        {
            // 标记为成功结束，基础编辑器清理时不会执行回滚。
            _isAccepted = true;
            eventInputArgs.Output = Unit.Default;
            eventInputArgs.IsInputValid = true;
            eventInputArgs.IsInputCompleted = true;

            return;
        }

        // Escape 留给基础 GetterEditor 生成取消结果，CancelInput 负责回滚快照。
        eventInputArgs.IsInputValid = true;
        eventInputArgs.IsInputCompleted = false;
    }

    protected override void CancelInput()
    {
        // _isAccepted 为 false 时涵盖 Escape、超时和外部取消。
        if (_isAccepted == false) Edit.Document.RunDesign(RestoreOriginalROIStates);

        RemoveSelectionWindow();
        ClearSelection();

        ResetInteractionState();
    }

    private void BeginSelection(Point point, bool isToggleSelection)
    {
        _editorStateEnum = EditorStateEnum.Select;
        _isToggleSelection = isToggleSelection;

        // 普通选择替换旧选择，Ctrl 框选则在结束时逐项切换选择状态。
        if (_isToggleSelection == false) ClearSelection();

        RemoveSelectionWindow();
        _selectionWindow = new SelectionWindow(point, point);
        Edit.Document.Transients.Add(_selectionWindow);
    }

    private void CompleteSelection()
    {
        Guard.IsNotNull(_selectionWindow);

        var selectionWindow = _selectionWindow;
        RemoveSelectionWindow();

        var selectedItems = GetSelectionFromWindow(selectionWindow);
        if (_isToggleSelection == false) ClearSelection();

        foreach (var rectROIDrawable in selectedItems)
        {
            // Ctrl 框选支持同时加入多个 ROI，也支持再次框选取消这些 ROI。
            if (_isToggleSelection)
            {
                ToggleSelection(rectROIDrawable);
            }
            else
            {
                Select(rectROIDrawable);
            }
        }

        UpdateEditorState();
    }

    private BitmapImageROIDrawable[] GetSelectionFromWindow(SelectionWindow selectionWindow)
    {
        var rectROIDrawables = GetVisibleRectROIDrawables();

        if (selectionWindow.StartPoint == selectionWindow.EndPoint)
        {
            var selectionPickDistance = Edit.Document.View.ScreenToWorldDistance(Edit.Document.Settings.SelectionPickDistance);
            var item = rectROIDrawables.FirstOrDefault(t => t.Contains(selectionWindow.StartPoint, selectionPickDistance));

            return item is null ? [] : [item];
        }

        var selectionWindowExtents = selectionWindow.GetExtents();

        // 框选不区分拖拽方向，只要 ROI 与选择框相交就选中。
        return [.. rectROIDrawables.Where(t => selectionWindowExtents.IntersectsWith(t.GetExtents()))];
    }

    private bool TryGetModifyTarget(Point point, out BitmapImageROIDrawable? target, out ControlPoint? controlPoint)
    {
        target = null;
        controlPoint = null;

        var selectedRectROIDrawables = Edit.SelectedItems.OfType<BitmapImageROIDrawable>().ToArray();
        if (selectedRectROIDrawables.Length == 0) return false;

        var controlPointPickDistance = Edit.Document.View.ScreenToWorldDistance(Edit.Document.Settings.ControlPointPickDistance);

        // 先命中锚点，再命中 ROI 本体，避免拖拽锚点被识别为移动操作。
        foreach (var rectROIDrawable in selectedRectROIDrawables)
        {
            var selectedControlPoint = rectROIDrawable.GetControlPoints()
                .FirstOrDefault(t => (t.BasePoint - point).Length <= controlPointPickDistance);

            if (selectedControlPoint is null) continue;

            target = rectROIDrawable;
            controlPoint = selectedControlPoint;

            return true;
        }

        foreach (var rectROIDrawable in selectedRectROIDrawables)
        {
            if (rectROIDrawable.Contains(point, controlPointPickDistance) == false) continue;

            target = rectROIDrawable;

            return true;
        }

        return false;
    }

    private void BeginModification(BitmapImageROIDrawable target, ControlPoint? controlPoint, Point point)
    {
        _editorStateEnum = EditorStateEnum.Modify;
        _editRectROIDrawable = target;
        _dragStartPoint = point;
        _dragOriginalRects.Clear();

        foreach (var rectROIDrawable in Edit.SelectedItems.OfType<BitmapImageROIDrawable>())
        {
            _dragOriginalRects[rectROIDrawable] = rectROIDrawable.Rect;
        }

        if (controlPoint is null)
        {
            _roiOperationModeEnum = ROIOperationModeEnum.Move;
            _resizeJoystickStateEnum = ResizeJoystickStateEnum.None;
            Edit.Document.View.CanvasControl?.CursorTypeEnum = CursorTypeEnum.SizeAll;

            return;
        }

        _roiOperationModeEnum = ROIOperationModeEnum.Resize;
        (_resizeJoystickStateEnum, var cursorTypeEnum) = GetResizeConfiguration(controlPoint.Name);

        Edit.Document.View.CanvasControl?.CursorTypeEnum = cursorTypeEnum;
    }

    private static (ResizeJoystickStateEnum ResizeJoystickStateEnum, CursorTypeEnum CursorTypeEnum) GetResizeConfiguration(string controlPointName)
    {
        return controlPointName switch
        {
            nameof(Rect.XMaxYMax) => (ResizeJoystickStateEnum.RightTop, CursorTypeEnum.SizeNESW),
            nameof(Rect.XMinYMax) => (ResizeJoystickStateEnum.LeftTop, CursorTypeEnum.SizeNWSE),
            nameof(Rect.XMinYMin) => (ResizeJoystickStateEnum.LeftBottom, CursorTypeEnum.SizeNESW),
            nameof(Rect.XMaxYMin) => (ResizeJoystickStateEnum.RightBottom, CursorTypeEnum.SizeNWSE),
            nameof(Rect.XCenterYMax) => (ResizeJoystickStateEnum.Top, CursorTypeEnum.SizeNS),
            nameof(Rect.XMinYCenter) => (ResizeJoystickStateEnum.Left, CursorTypeEnum.SizeWE),
            nameof(Rect.XCenterYMin) => (ResizeJoystickStateEnum.Bottom, CursorTypeEnum.SizeNS),
            nameof(Rect.XMaxYCenter) => (ResizeJoystickStateEnum.Right, CursorTypeEnum.SizeWE),
            _ => throw new ArgumentOutOfRangeException(nameof(controlPointName), controlPointName, "Unknown rectangle control point.")
        };
    }

    private void ApplyModification(Point currentPoint)
    {
        if (_dragOriginalRects.Count == 0) return;

        var imageRect = GetImageRect();
        if (imageRect.IsEmpty) return;

        var delta = currentPoint - _dragStartPoint;

        switch (_roiOperationModeEnum)
        {
            case ROIOperationModeEnum.Move:
                ApplyMove(delta, imageRect);
                break;

            case ROIOperationModeEnum.Resize:
                ApplyResize(delta, imageRect);
                break;
        }
    }

    private void ApplyMove(Vector delta, Rect imageRect)
    {
        // 多选移动使用共同合法位移，保持每个 ROI 之间的相对位置。
        var constrainedDelta = ConstrainMoveDelta(delta, imageRect);

        foreach (var (rectROIDrawable, originalRect) in _dragOriginalRects)
        {
            rectROIDrawable.Rect = ClampRectToImage(originalRect + constrainedDelta, imageRect);
        }
    }

    private void ApplyResize(Vector delta, Rect imageRect)
    {
        // 多选缩放使用相同锚点和拖动向量，但每个 ROI 独立限制在图片内。
        foreach (var (rectROIDrawable, originalRect) in _dragOriginalRects)
        {
            var modifiedRect = ResizeRect(originalRect, _resizeJoystickStateEnum, delta);
            rectROIDrawable.Rect = ClampRectToImage(modifiedRect, imageRect);
        }
    }

    private Vector ConstrainMoveDelta(Vector delta, Rect imageRect)
    {
        // 用全部选中 ROI 的外接范围计算共同位移边界，避免任何对象越过图片边缘。
        var originalRects = _dragOriginalRects.Values.ToArray();
        var minX = originalRects.Min(t => t.XMin);
        var maxX = originalRects.Max(t => t.XMax);
        var minY = originalRects.Min(t => t.YMin);
        var maxY = originalRects.Max(t => t.YMax);

        return new Vector(
            Math.Clamp(delta.X, imageRect.XMin - minX, imageRect.XMax - maxX),
            Math.Clamp(delta.Y, imageRect.YMin - minY, imageRect.YMax - maxY));
    }

    private static Rect ResizeRect(Rect rect, ResizeJoystickStateEnum resizeJoystickStateEnum, Vector delta)
    {
        return resizeJoystickStateEnum switch
        {
            ResizeJoystickStateEnum.LeftTop => (Rect)new Extents(
                new Point(rect.XMin, rect.YMax) + delta,
                new Point(rect.XMax, rect.YMin)),
            ResizeJoystickStateEnum.Top => (Rect)new Extents(
                new Point(rect.XMax, rect.YMax) + new Vector(0, delta.Y),
                new Point(rect.XMin, rect.YMin)),
            ResizeJoystickStateEnum.RightTop => (Rect)new Extents(
                new Point(rect.XMax, rect.YMax) + delta,
                new Point(rect.XMin, rect.YMin)),
            ResizeJoystickStateEnum.Right => (Rect)new Extents(
                new Point(rect.XMax, rect.YMax) + new Vector(delta.X, 0),
                new Point(rect.XMin, rect.YMin)),
            ResizeJoystickStateEnum.RightBottom => (Rect)new Extents(
                new Point(rect.XMax, rect.YMin) + delta,
                new Point(rect.XMin, rect.YMax)),
            ResizeJoystickStateEnum.Bottom => (Rect)new Extents(
                new Point(rect.XMin, rect.YMin) + new Vector(0, delta.Y),
                new Point(rect.XMax, rect.YMax)),
            ResizeJoystickStateEnum.LeftBottom => (Rect)new Extents(
                new Point(rect.XMin, rect.YMin) + delta,
                new Point(rect.XMax, rect.YMax)),
            ResizeJoystickStateEnum.Left => (Rect)new Extents(
                new Point(rect.XMin, rect.YMin) + new Vector(delta.X, 0),
                new Point(rect.XMax, rect.YMax)),
            ResizeJoystickStateEnum.None => rect,
            _ => throw new ArgumentOutOfRangeException(nameof(resizeJoystickStateEnum), resizeJoystickStateEnum, "Unknown rectangle resize operation.")
        };
    }

    private Rect GetImageRect()
    {
        var bitmapImage = Options.BitmapImageDrawable.BitmapImage;
        if (bitmapImage?.IsEmpty != false) return Rect.Empty;

        return new Rect(
            Options.BitmapImageDrawable.Point,
            new Size(bitmapImage.Width, bitmapImage.Height));
    }

    private static Rect ClampRectToImage(Rect rect, Rect imageRect)
    {
        // 统一处理初始化、移动和缩放，确保 ROI 始终位于图片范围内。
        var xMin = Math.Clamp(Math.Min(rect.XMin, rect.XMax), imageRect.XMin, imageRect.XMax);
        var xMax = Math.Clamp(Math.Max(rect.XMin, rect.XMax), imageRect.XMin, imageRect.XMax);
        var yMin = Math.Clamp(Math.Min(rect.YMin, rect.YMax), imageRect.YMin, imageRect.YMax);
        var yMax = Math.Clamp(Math.Max(rect.YMin, rect.YMax), imageRect.YMin, imageRect.YMax);

        return new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
    }

    private BitmapImageROIDrawable[] GetRectROIDrawables() => Edit.Document.OverlayerModel.OfType<BitmapImageROIDrawable>().ToArray();

    private BitmapImageROIDrawable[] GetVisibleRectROIDrawables() => Edit.Document.View.VisibleItems.OfType<BitmapImageROIDrawable>().ToArray();

    private void RestoreOriginalROIStates()
    {
        // 恢复矩形和修改标记，保证取消编辑不会留下任何状态变化。
        foreach (var (rectROIDrawable, state) in _originalROIStates)
        {
            rectROIDrawable.Rect = state.Rect;
            rectROIDrawable.IsModified = state.IsModified;
        }
    }

    private void ClearSelection()
    {
        foreach (var item in Edit.SelectedItems) item.IsSelected = false;

        Edit.SelectedItems.Clear();
    }

    private void Select(BitmapImageROIDrawable bitmapImageROIDrawable)
    {
        if (Edit.SelectedItems.Contains(bitmapImageROIDrawable)) return;

        bitmapImageROIDrawable.IsSelected = true;
        Edit.SelectedItems.Add(bitmapImageROIDrawable);
    }

    private void ToggleSelection(BitmapImageROIDrawable bitmapImageROIDrawable)
    {
        // SelectedItems 与 drawable.IsSelected 必须同步维护，CanvasView 依赖二者绘制选择状态。
        if (Edit.SelectedItems.Contains(bitmapImageROIDrawable))
        {
            bitmapImageROIDrawable.IsSelected = false;
            Edit.SelectedItems.Remove(bitmapImageROIDrawable);
        }
        else
        {
            Select(bitmapImageROIDrawable);
        }

        UpdateEditorState();
    }

    private void UpdateEditorState()
    {
        _editorStateEnum = Edit.SelectedItems.OfType<BitmapImageROIDrawable>().Any()
            ? EditorStateEnum.Modify
            : EditorStateEnum.Select;
    }

    private void ResetInteractionState()
    {
        _isCursorDown = false;
        _editRectROIDrawable = null;
        _dragOriginalRects.Clear();
        _roiOperationModeEnum = ROIOperationModeEnum.None;
        _resizeJoystickStateEnum = ResizeJoystickStateEnum.None;
        _isToggleSelection = false;
        Edit.Document.View.CanvasControl?.CursorTypeEnum = _defaultCursorTypeEnum;
    }

    private void RemoveSelectionWindow()
    {
        if (_selectionWindow is null) return;

        Edit.Document.Transients.Remove(_selectionWindow);
        _selectionWindow = null;
    }

    private enum EditorStateEnum
    {
        Select,
        Modify
    }

    private enum ResizeJoystickStateEnum
    {
        None,
        LeftTop,
        Top,
        RightTop,
        Right,
        RightBottom,
        Bottom,
        LeftBottom,
        Left
    }

    private enum ROIOperationModeEnum
    {
        None,
        Move,
        Resize
    }
}
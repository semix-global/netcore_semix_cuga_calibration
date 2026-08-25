using System.Collections.Immutable;
using CommunityToolkit.Diagnostics;
using Net.Utilities.Graphics;
using Net.Utilities.Graphics.Editors;
using Net.Utilities.Graphics.Extensions;
using Net.Utilities.Graphics.Primitives.Editors.Getters;
using Net.Utilities.Graphics.Primitives.Enums.Inputs;
using Net.Utilities.Graphics.Primitives.EventArgs.Inputs;
using Net.Utilities.ImageViewer.WPF.Drawables;
using Net.Utilities.ImageViewer.WPF.Extensions;
using Net.Utilities.ImageViewer.WPF.Primitives.Enums;
using Net.Utilities.Models;
using Net.Utilities.Models.Geometries;

namespace Net.Utilities.OpticsFourierImageViewer.WPF.Editors;

public sealed class AddOrModifyRectROIDrawableGetterEditor(
    CanvasEdit edit,
    AddOrModifyRectROIDrawableInputOptions options,
    TaskCompletionSource<OutputResult<Unit>> completion) : GetterEditor<AddOrModifyRectROIDrawableInputOptions, Unit>(edit, options, completion)
{
    private readonly ROIExtents _roiExtents = new();

    private RectROIDrawable? _editRectROIDrawable;
    private CursorTypeEnum _defaultCursorTypeEnum;
    private EditorStateEnum _editorStateEnum;
    private ROIOperationModeEnum _roiOperationModeEnum;
    private ResizeJoystickStateEnum _resizeJoystickStateEnum;

    protected override void Init(InitArgs<Unit> args)
    {
        base.Init(args);

        _roiExtents.IsCursorDown = false;
        _roiExtents.StartPoint = _roiExtents.EndPoint = Point.Origin;
        _editorStateEnum = EditorStateEnum.Add;

        Edit.SelectedItems.Clear();

        var rectROIDrawables = Edit.Document.OverlayerModel.OfType<RectROIDrawable>().ToImmutableArray();
        if (Options.IsMultiple == false && rectROIDrawables.Length > 1) Edit.Document.OverlayerModel.RemoveRange(rectROIDrawables.Skip(1));
    }

    protected override void CursorChanged(Point point)
    {
        DoPrompt(point.ToString(Edit.Document.Settings.NumberFormat));

        if (_roiExtents.IsCursorDown == false) return;

        _roiExtents.EndPoint = point.ImageCoordinateRound();
        if (_editRectROIDrawable is null) return;

        var vector = _roiExtents.EndPoint - _roiExtents.StartPoint;
        if (_editorStateEnum != EditorStateEnum.Add) _roiExtents.StartPoint = _roiExtents.EndPoint;

        _editRectROIDrawable.Rect = _editorStateEnum switch
        {
            EditorStateEnum.Add => (Rect)_roiExtents.Extents,
            EditorStateEnum.Modify => _roiOperationModeEnum switch
            {
                ROIOperationModeEnum.None => _editRectROIDrawable.Rect,
                ROIOperationModeEnum.Move => _editRectROIDrawable.Rect + vector,
                ROIOperationModeEnum.Resize => _resizeJoystickStateEnum switch
                {
                    ResizeJoystickStateEnum.None => _editRectROIDrawable.Rect,
                    ResizeJoystickStateEnum.LeftTop => (Rect)new Extents
                    (
                        new Point(_editRectROIDrawable.Rect.XMin, _editRectROIDrawable.Rect.YMax) + vector,
                        new Point(_editRectROIDrawable.Rect.XMax, _editRectROIDrawable.Rect.YMin)
                    ),
                    ResizeJoystickStateEnum.Top => (Rect)new Extents
                    (
                        new Point(_editRectROIDrawable.Rect.XMax, _editRectROIDrawable.Rect.YMax) + vector.WithX(0),
                        new Point(_editRectROIDrawable.Rect.XMin, _editRectROIDrawable.Rect.YMin)
                    ),
                    ResizeJoystickStateEnum.RightTop => (Rect)new Extents
                    (
                        new Point(_editRectROIDrawable.Rect.XMax, _editRectROIDrawable.Rect.YMax) + vector,
                        new Point(_editRectROIDrawable.Rect.XMin, _editRectROIDrawable.Rect.YMin)
                    ),
                    ResizeJoystickStateEnum.Right => (Rect)new Extents
                    (
                        new Point(_editRectROIDrawable.Rect.XMax, _editRectROIDrawable.Rect.YMax) + vector.WithY(0),
                        new Point(_editRectROIDrawable.Rect.XMin, _editRectROIDrawable.Rect.YMin)
                    ),
                    ResizeJoystickStateEnum.RightBottom => (Rect)new Extents
                    (
                        new Point(_editRectROIDrawable.Rect.XMax, _editRectROIDrawable.Rect.YMin) + vector,
                        new Point(_editRectROIDrawable.Rect.XMin, _editRectROIDrawable.Rect.YMax)
                    ),
                    ResizeJoystickStateEnum.Bottom => (Rect)new Extents
                    (
                        new Point(_editRectROIDrawable.Rect.XMin, _editRectROIDrawable.Rect.YMin) + vector.WithX(0),
                        new Point(_editRectROIDrawable.Rect.XMax, _editRectROIDrawable.Rect.YMax)
                    ),
                    ResizeJoystickStateEnum.LeftBottom => (Rect)new Extents
                    (
                        new Point(_editRectROIDrawable.Rect.XMin, _editRectROIDrawable.Rect.YMin) + vector,
                        new Point(_editRectROIDrawable.Rect.XMax, _editRectROIDrawable.Rect.YMax)
                    ),
                    ResizeJoystickStateEnum.Left => (Rect)new Extents
                    (
                        new Point(_editRectROIDrawable.Rect.XMin, _editRectROIDrawable.Rect.YMin) + vector.WithY(0),
                        new Point(_editRectROIDrawable.Rect.XMax, _editRectROIDrawable.Rect.YMax)
                    ),
                    _ => ThrowHelper.ThrowArgumentOutOfRangeException<Rect>(nameof(_resizeJoystickStateEnum))
                },
                _ => ThrowHelper.ThrowArgumentOutOfRangeException<Rect>(nameof(_roiOperationModeEnum))
            },
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<Rect>(nameof(_editRectROIDrawable))
        };
    }

    protected override void CursorDownInput(EventInputArgs<CursorEventArgs, Unit> eventInputArgs)
    {
        _defaultCursorTypeEnum = Edit.Document.View.CanvasControl?.CursorTypeEnum ?? CursorTypeEnum.Arrow;

        if (eventInputArgs.CheckIsCursorButtonEnum(CursorButtonEnum.Left, CursorButtonStateEnum.Pressed) == false) return;

        _roiExtents.IsCursorDown = true;
        _roiExtents.StartPoint = _roiExtents.EndPoint = eventInputArgs.Event.Point.ImageCoordinateRound();

        switch (_editorStateEnum)
        {
            case EditorStateEnum.Add:
                Add:
                Edit.SelectedItems.Clear();

                if (Options.IsMultiple || Edit.Document.OverlayerModel.OfType<RectROIDrawable>().Any() == false)
                {
                    _editRectROIDrawable = new RectROIDrawable();
                    Edit.Document.Transients.Add(_editRectROIDrawable);
                }
                else _editRectROIDrawable = null;

                break;

            case EditorStateEnum.Modify:
                if (_editRectROIDrawable is null)
                {
                    _editorStateEnum = EditorStateEnum.Add;
                    goto Add;
                }

                var controlPointSize = Edit.Document.View.ScreenToWorldDistance(Edit.Document.Settings.ControlPointPickDistance);
                var controlPoint = _editRectROIDrawable.GetControlPoints()
                    .Select(t => (ControlPoint: t, Contains: (t.BasePoint - eventInputArgs.Event.Point).Length <= controlPointSize))
                    .Where(t => t.Contains)
                    .Select(t => t.ControlPoint)
                    .FirstOrDefault();

                if (Edit.SelectedItems.Contains(_editRectROIDrawable) == false
                    || (_editRectROIDrawable.Contains(eventInputArgs.Event.Point, controlPointSize) == false && controlPoint is null))
                {
                    _editorStateEnum = EditorStateEnum.Add;
                    goto Add;
                }

                (_roiOperationModeEnum, _resizeJoystickStateEnum, var cursorTypeEnum) = controlPoint?.Name switch
                {
                    nameof(Rect.XMaxYMax) => (ROIOperationModeEnum.Resize, ResizeJoystickStateEnum.RightTop, CursorTypeEnum.SizeNESW),
                    nameof(Rect.XMinYMax) => (ROIOperationModeEnum.Resize, ResizeJoystickStateEnum.LeftTop, CursorTypeEnum.SizeNWSE),
                    nameof(Rect.XMinYMin) => (ROIOperationModeEnum.Resize, ResizeJoystickStateEnum.LeftBottom, CursorTypeEnum.SizeNESW),
                    nameof(Rect.XMaxYMin) => (ROIOperationModeEnum.Resize, ResizeJoystickStateEnum.RightBottom, CursorTypeEnum.SizeNWSE),
                    nameof(Rect.XCenterYMax) => (ROIOperationModeEnum.Resize, ResizeJoystickStateEnum.Top, CursorTypeEnum.SizeNS),
                    nameof(Rect.XMinYCenter) => (ROIOperationModeEnum.Resize, ResizeJoystickStateEnum.Left, CursorTypeEnum.SizeWE),
                    nameof(Rect.XCenterYMin) => (ROIOperationModeEnum.Resize, ResizeJoystickStateEnum.Bottom, CursorTypeEnum.SizeNS),
                    nameof(Rect.XMaxYCenter) => (ROIOperationModeEnum.Resize, ResizeJoystickStateEnum.Right, CursorTypeEnum.SizeWE),
                    null => (ROIOperationModeEnum.Move, ResizeJoystickStateEnum.None, CursorTypeEnum.SizeAll),
                    _ => (_roiOperationModeEnum, _resizeJoystickStateEnum, Edit.Document.View.CanvasControl?.CursorTypeEnum ?? CursorTypeEnum.Arrow)
                };

                Edit.Document.View.CanvasControl?.CursorTypeEnum = cursorTypeEnum;
                break;

            default:
                ThrowHelper.ThrowArgumentOutOfRangeException(nameof(_editorStateEnum));
                break;
        }

        eventInputArgs.IsInputValid = true;
        eventInputArgs.IsInputCompleted = false;
    }

    protected override void CursorUpInput(EventInputArgs<CursorEventArgs, Unit> eventInputArgs)
    {
        Edit.Document.View.CanvasControl?.CursorTypeEnum = _defaultCursorTypeEnum;
        _roiOperationModeEnum = ROIOperationModeEnum.None;
        _resizeJoystickStateEnum = ResizeJoystickStateEnum.None;

        eventInputArgs.IsInputValid = true;
        eventInputArgs.IsInputCompleted = false;

        if (_editRectROIDrawable is not null) Edit.Document.Transients.Remove(_editRectROIDrawable);

        if (_roiExtents.IsCursorDown == false) return;
        _roiExtents.IsCursorDown = false;

        switch (_editorStateEnum)
        {
            case EditorStateEnum.Add:
                if (_roiExtents.Extents.Size.Area == 0)
                {
                    var controlPointSize = Edit.Document.View.ScreenToWorldDistance(Edit.Document.Settings.ControlPointPickDistance);
                    _editRectROIDrawable = Edit.Document.OverlayerModel
                        .OfType<RectROIDrawable>()
                        .FirstOrDefault(t => t.Contains(_roiExtents.StartPoint, controlPointSize));
                    if (_editRectROIDrawable is not null)
                    {
                        Edit.SelectedItems.Add(_editRectROIDrawable);
                        _editorStateEnum = EditorStateEnum.Modify;
                    }
                }
                else if (_editRectROIDrawable is not null)
                {
                    Edit.Document.OverlayerModel.Add(_editRectROIDrawable);

                    _editRectROIDrawable = null;

                    if (Options.IsMultiple == false) eventInputArgs.IsInputCompleted = true;
                }

                break;

            case EditorStateEnum.Modify:
                if (_editRectROIDrawable is null) return;
                if (_editRectROIDrawable.Rect.Size.Area == 0)
                {
                    Edit.Document.OverlayerModel.Remove(_editRectROIDrawable);
                    Edit.SelectedItems.Clear();

                    _editRectROIDrawable = null;

                    if (Options.IsMultiple == false) eventInputArgs.IsInputCompleted = true;
                }

                break;

            default:
                ThrowHelper.ThrowArgumentOutOfRangeException(nameof(_editorStateEnum));
                break;
        }
    }

    protected override void CancelInput()
    {
        if (_editRectROIDrawable is not null) Edit.Document.Transients.Remove(_editRectROIDrawable);

        Edit.SelectedItems.Clear();
    }

    private sealed class ROIExtents
    {
        public bool IsCursorDown { get; set; }

        public Point StartPoint { get; set; }

        public Point EndPoint { get; set; }

        public Extents Extents => new(StartPoint, EndPoint);
    }
}
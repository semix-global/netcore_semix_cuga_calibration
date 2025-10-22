using ScottPlot;
using ScottPlot.Interactivity;
using ScottPlot.Interactivity.UserActionResponses;

namespace Core.Utilities.WPF.ScottPlot.Interactivity.UserActionResponses;

internal sealed class MouseDragPanOrZoomRectangle(MouseButton button, Key dragZoomRectangleKey) : IUserActionResponse
{
    private readonly MouseDragPan _mouseDragPan = new(button)
    {
        LockY = false,
        LockX = false,
        ChangeOpposingAxesTogether = false
    };

    private readonly MouseDragZoomRectangle _mouseDragZoomRectangle = new(button)
    {
        SecondaryMouseButton = new MouseButton(string.Empty),
        SecondaryKey = new Key(string.Empty)
    };

    public MouseButton MouseButton { get; } = button;

    public Key DragZoomRectangleKey { get; } = dragZoomRectangleKey;

    #region MouseDragPan

    public bool LockY
    {
        get => _mouseDragPan.LockY;
        set => _mouseDragPan.LockY = value;
    }

    public bool LockX
    {
        get => _mouseDragPan.LockX;
        set => _mouseDragPan.LockX = value;
    }

    public bool ChangeOpposingAxesTogether
    {
        get => _mouseDragPan.ChangeOpposingAxesTogether;
        set => _mouseDragPan.ChangeOpposingAxesTogether = value;
    }

    #endregion MouseDragPan

    #region MouseDragZoomRectangle

    public Key MouseDragZoomRectangleHorizontalLockKey
    {
        get => _mouseDragZoomRectangle.HorizontalLockKey;
        set => _mouseDragZoomRectangle.HorizontalLockKey = value;
    }

    public Key MouseDragZoomRectangleVerticalLockKey
    {
        get => _mouseDragZoomRectangle.VerticalLockKey;
        set => _mouseDragZoomRectangle.VerticalLockKey = value;
    }

    #endregion MouseDragZoomRectangle

    public void ResetState(IPlotControl plotControl)
    {
        _mouseDragPan.ResetState(plotControl);
        _mouseDragZoomRectangle.ResetState(plotControl);
    }

    public ResponseInfo Execute(IPlotControl plotControl, IUserAction userInput, KeyboardState keys)
    {
        return keys.IsPressed(DragZoomRectangleKey)
            ? _mouseDragZoomRectangle.Execute(plotControl, userInput, keys)
            : _mouseDragPan.Execute(plotControl, userInput, keys);
    }
}
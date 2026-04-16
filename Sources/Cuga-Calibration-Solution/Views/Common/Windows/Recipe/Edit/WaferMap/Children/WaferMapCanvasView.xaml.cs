using System.Windows.Input;

namespace CugaCalibration.Views.Common.Windows.Recipe.Edit.WaferMap.Children;

public partial class WaferMapCanvasView
{
    public WaferMapCanvasView()
    {
        InitializeComponent();

        WaferMapCanvas.AddHandler(
            MouseRightButtonUpEvent,
            new MouseButtonEventHandler(OnWaferMapCanvasRightButtonUp),
            handledEventsToo: true
        );
    }

    private void OnWaferMapCanvasRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (WaferMapCanvas.ContextMenu is { } menu)
        {
            menu.PlacementTarget = WaferMapCanvas;
            menu.DataContext = WaferMapCanvas.DataContext; // 不影响 BindingProxy 方案
            menu.IsOpen = true;
            e.Handled = true;
        }
    }
}
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CugaCalibration.Views.Common.Windows.Recipe.Edit.WaferMap.Children;

public partial class WaferMapCanvasView
{
    /// <summary>
    /// GotoPositionCommand 依赖属性
    /// </summary>
    public static readonly DependencyProperty GotoPositionCommandProperty =
        DependencyProperty.Register(
            nameof(GotoPositionCommand),
            typeof(ICommand),
            typeof(WaferMapCanvasView),
            new PropertyMetadata(null));

    /// <summary>
    /// Goto Position Command - 从外部绑定
    /// </summary>
    public ICommand? GotoPositionCommand
    {
        get => (ICommand?)GetValue(GotoPositionCommandProperty);
        set => SetValue(GotoPositionCommandProperty, value);
    }

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

            // 手动设置 MenuItem 的 Command，因为 ContextMenu 不在可视化树中
            foreach (var item in menu.Items)
            {
                if (item is MenuItem menuItem && menuItem.Header?.ToString() == "Goto Position")
                {
                    System.Diagnostics.Debug.WriteLine($"Setting command on MenuItem, GotoPositionCommand type: {GotoPositionCommand?.GetType().FullName}");
                    menuItem.Command = GotoPositionCommand;
                    System.Diagnostics.Debug.WriteLine($"MenuItem Command set, Command is null: {menuItem.Command == null}");
                }
            }

            menu.IsOpen = true;
            e.Handled = true;
        }
    }
}
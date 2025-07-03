using CugaCalibration.ViewModels.Common.Windows.Management.Recipe;
using Microsoft.Extensions.Logging;
using Net.Utilities.WPF.MVVM;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Point = Net.Utilities.Models.Geometries.Point;
using Vector = Net.Utilities.Models.Geometries.Vector;

namespace CugaCalibration.Views.Common.Windows.Management.Recipe.Children;

public partial class WaferMapCanvasView
{
    private readonly ILogger<WaferMapCanvasView> _logger;
    private ContextMenu _waferMapContextMenu;
    private bool _isLoaded;

    public WaferMapCanvasView()
    {
        InitializeComponent();

        _logger = HostApplication.GetRequiredService<ILogger<WaferMapCanvasView>>();

        Loaded -= OnLoaded;
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is not RecipeSettingViewModel viewModel) return;
        if (_isLoaded) return;
        _isLoaded = true;

        // 创建右键菜单
        _waferMapContextMenu = new ContextMenu();

        var menuItem = new MenuItem { Header = "Goto Die Position" };
        menuItem.Click += MenuItem_Refresh_Click;

        _waferMapContextMenu.Items.Add(menuItem);

        // 监听鼠标右键
        WaferMapCanvas.PreviewMouseRightButtonUp += WaferMapCanvas_PreviewMouseRightButtonUp;
    }

    private void WaferMapCanvas_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        // 获取鼠标位置并弹出菜单
        _waferMapContextMenu.PlacementTarget = WaferMapCanvas;
        _waferMapContextMenu.IsOpen = true;
    }

    private void MenuItem_Refresh_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not RecipeSettingViewModel viewModel) return;
        var document = viewModel.WaferMapCanvasViewModel.Document;
        if (document.ActiveView is null) return;

        if (viewModel._selectionDies is null) return;

        var index = viewModel._selectionDies.ElementAt(0).Index;
        var centerPosition = viewModel.CalibrationRecipeDto.WaferDto.WaferCenterBrightFieldPosition!;
        var waferPosition = new Point(document.OriginalDie.Rect.X + index.X * document.DieBuilder.DieSize.Width,
            document.OriginalDie.Rect.Y + index.Y * document.DieBuilder.DieSize.Height);
        var position = centerPosition.Value + (Vector)waferPosition;

        viewModel.StageViewModel.SetBrightFieldAbsoluteStageXy(position);
    }
}
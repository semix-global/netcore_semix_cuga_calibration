using Core.Models.Models.Common.Recipe.Wafer.ReticleMask;
using CugaCalibration.ViewModels.Common;
using Microsoft.Xaml.Behaviors;
using Net.Utilities.WPF.MVVM;
using ScottPlot;
using ScottPlot.Interactivity;
using ScottPlot.Interactivity.UserActionResponses;
using ScottPlot.Plottables;
using ScottPlot.WPF;
using System.Collections.ObjectModel;
using System.Windows;
using Point = Net.Utilities.Models.Geometries.Point;
using Size = Net.Utilities.Models.Geometries.Size;

namespace CugaCalibration.Core.Behaviors;

public sealed class WpfPlotReticleMaskBehavior : Behavior<WpfPlot>
{
    private StageViewModel? _stageViewModel;

    public Point WaferCenterBrightFieldPosition
    {
        get => (Point)GetValue(WaferCenterBrightFieldPositionProperty);
        set => SetValue(WaferCenterBrightFieldPositionProperty, value);
    }

    public static readonly DependencyProperty WaferCenterBrightFieldPositionProperty = DependencyProperty.Register(
        nameof(WaferCenterBrightFieldPosition),
        typeof(Point),
        typeof(WpfPlotReticleMaskBehavior),
        new PropertyMetadata(new Point(), PropertyChangedCallback)
    );

    public Size DieSize
    {
        get => (Size)GetValue(DieSizeProperty);
        set => SetValue(DieSizeProperty, value);
    }

    public static readonly DependencyProperty DieSizeProperty = DependencyProperty.Register(
        nameof(DieSize),
        typeof(Size),
        typeof(WpfPlotReticleMaskBehavior),
        new PropertyMetadata(Size.Empty, PropertyChangedCallback)
    );

    public ObservableCollection<ReticleMarkItemDto> ReticleMaskList
    {
        get => (ObservableCollection<ReticleMarkItemDto>)GetValue(ReticleMaskListProperty);
        set => SetValue(ReticleMaskListProperty, value);
    }

    public static readonly DependencyProperty ReticleMaskListProperty = DependencyProperty.Register(
        nameof(ReticleMaskList),
        typeof(ObservableCollection<ReticleMarkItemDto>),
        typeof(WpfPlotReticleMaskBehavior),
        new PropertyMetadata(null, PropertyChangedCallback)
    );

    private static void PropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs _)
    {
        if (d is not WpfPlotReticleMaskBehavior behaviors || behaviors.AssociatedObject is null) return;

        behaviors.Update();
    }

    private Rectangle? _reticleRectangle;

    public WpfPlotReticleMaskBehavior()
    {
        ReticleMaskList = [];
    }

    protected override void OnAttached()
    {
        base.OnAttached();

        AssociatedObject.Plot.DataBackground = new BackgroundStyle { Color = Colors.Transparent };
        AssociatedObject.Plot.FigureBackground = new BackgroundStyle { Color = Colors.White };
        AssociatedObject.UserInputProcessor.IsEnabled = true;
        AssociatedObject.UserInputProcessor.UserActionResponses.Add(new DoubleClickResponse(StandardMouseButtons.Left, (plotControl, pixel) =>
        {
            var point = plotControl.GetPlotAtPixel(pixel)?.GetCoordinates(pixel);
            if (point is null) return;

            _stageViewModel ??= HostApplication.GetRequiredService<StageViewModel>();

            _stageViewModel.SetBrightFieldAbsoluteStageXyByNotAutoFocus(new Point(point.Value.X, point.Value.Y));
        }));

        AssociatedObject.Plot.HideAxesAndGrid();

        Update();
    }

    private void Update()
    {
        try
        {
            AssociatedObject.Plot.Clear();

            const float originMaxLengthPixel = 30;

            AssociatedObject.Plot.Axes.SetLimits(-DieSize.Width * 1.1, DieSize.Width * 1.1, -DieSize.Height * 1.1, DieSize.Height * 1.1);
            _reticleRectangle = AssociatedObject.Plot.Add.Rectangle(new CoordinateRect(new Coordinates(0, 0), new CoordinateSize(DieSize.Width, DieSize.Height)));
            _reticleRectangle.LineColor = Colors.Transparent;
            _reticleRectangle.FillColor = Color.FromHex("#F0F0F0");

            var positionMarker1 = AssociatedObject.Plot.Add.Marker(0, 0, shape: MarkerShape.Cross, size: originMaxLengthPixel * 3, color: Colors.Red);
            positionMarker1.LineWidth = 2;

            var positionMarker2 = AssociatedObject.Plot.Add.Marker(0, 0, shape: MarkerShape.OpenCircle, size: originMaxLengthPixel * 1.5f, color: Colors.Red);
            positionMarker2.LineWidth = 2;

            foreach (var reticleMarkItemDto in ReticleMaskList ?? [])
            {
                var marker = AssociatedObject.Plot.Add.Marker(0, 0, shape: MarkerShape.Cross, size: originMaxLengthPixel * 3, color: Colors.Red);
                marker.X = reticleMarkItemDto.MaskWaferCellPosition.X;
                marker.Y = reticleMarkItemDto.MaskWaferCellPosition.Y;

                AssociatedObject.Plot.PlottableList.Add(new Text
                {
                    LabelText = $"{reticleMarkItemDto.MaskIndex}",
                    LabelBackgroundColor = Colors.Transparent,
                    Location = new Coordinates(reticleMarkItemDto.MaskWaferCellPosition.X, reticleMarkItemDto.MaskWaferCellPosition.Y),
                    LabelFontSize = 30,
                    LabelPadding = 1,
                    LabelFontColor = Colors.Blue,
                    LabelAlignment = Alignment.MiddleCenter,
                    OffsetX = 10,
                    OffsetY = 15
                });
            }
        }
        finally
        {
            AssociatedObject.Refresh();
            AssociatedObject.Plot.Axes.AutoScale();
        }
    }
}
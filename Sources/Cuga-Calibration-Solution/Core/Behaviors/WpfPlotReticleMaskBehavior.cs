using Core.Recipe.Models.Wafer;
using Core.Recipe.Models.Wafer.ReticleMask;
using Microsoft.Xaml.Behaviors;
using ScottPlot;
using ScottPlot.Plottables;
using ScottPlot.WPF;
using System.Collections.ObjectModel;
using System.Windows;
using Size = Net.Utilities.Models.Geometries.Size;

namespace CugaCalibration.Core.Behaviors;

public sealed class WpfPlotReticleMaskBehavior : Behavior<WpfPlot>
{
    public WaferDTO WaferDTO
    {
        get => (WaferDTO)GetValue(WaferDTOProperty);
        set => SetValue(WaferDTOProperty, value);
    }

    public static readonly DependencyProperty WaferDTOProperty = DependencyProperty.Register(
        nameof(WaferDTO),
        typeof(WaferDTO),
        typeof(WpfPlotReticleMaskBehavior),
        new PropertyMetadata(new WaferDTO(), PropertyChangedCallback)
    );

    public ObservableCollection<ReticleMarkDTOItem> ReticleMaskList
    {
        get => (ObservableCollection<ReticleMarkDTOItem>)GetValue(ReticleMaskListProperty);
        set => SetValue(ReticleMaskListProperty, value);
    }

    public static readonly DependencyProperty ReticleMaskListProperty = DependencyProperty.Register(
        nameof(ReticleMaskList),
        typeof(ObservableCollection<ReticleMarkDTOItem>),
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

        AssociatedObject.Plot.HideAxesAndGrid();

        Update();
    }

    private void Update()
    {
        try
        {
            AssociatedObject.Plot.Clear();

            const float originMaxLengthPixel = 30;

            var dieSize = new Size(WaferDTO.WaferMapDataDTO.DiePitchWidth, WaferDTO.WaferMapDataDTO.DiePitchHeight);
            AssociatedObject.Plot.Axes.SetLimits(-dieSize.Width * 1.1, dieSize.Width * 1.1, -dieSize.Height * 1.1, dieSize.Height * 1.1);
            _reticleRectangle = AssociatedObject.Plot.Add.Rectangle(new CoordinateRect(new Coordinates(0, 0), new CoordinateSize(dieSize.Width, dieSize.Height)));
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
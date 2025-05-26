using Core.Models.Enums.Stage;
using CugaCalibration.ViewModels.Common;
using Microsoft.Xaml.Behaviors;
using Net.Utilities.WPF.MVVM;
using ScottPlot;
using ScottPlot.Interactivity;
using ScottPlot.Interactivity.UserActionResponses;
using ScottPlot.Plottables;
using ScottPlot.WPF;
using System.Windows;
using Point = Net.Utilities.Models.Point;

namespace CugaCalibration.Core.Behaviors;

public sealed class WpfPlotWaferMapBehavior : Behavior<WpfPlot>
{
    private StageViewModel? _stageViewModel;

    #region 依赖属性

    public double Radius
    {
        get => (double)GetValue(RadiusProperty);
        set => SetValue(RadiusProperty, value);
    }

    public static readonly DependencyProperty RadiusProperty = DependencyProperty.Register(
        nameof(Radius),
        typeof(double),
        typeof(WpfPlotWaferMapBehavior),
        new PropertyMetadata(150000d, PropertyChangedCallback)
    );

    public StageCoordinateSystemEnum StageCoordinateSystemEnum
    {
        get => (StageCoordinateSystemEnum)GetValue(StageCoordinateSystemEnumProperty);
        set => SetValue(StageCoordinateSystemEnumProperty, value);
    }

    public static readonly DependencyProperty StageCoordinateSystemEnumProperty = DependencyProperty.Register(
        nameof(StageCoordinateSystemEnum),
        typeof(StageCoordinateSystemEnum),
        typeof(WpfPlotWaferMapBehavior),
        new PropertyMetadata(StageCoordinateSystemEnum.Bright, PropertyChangedCallback)
    );

    public Point BrightPosition
    {
        get => (Point)GetValue(BrightPositionProperty);
        set => SetValue(BrightPositionProperty, value);
    }

    public static readonly DependencyProperty BrightPositionProperty = DependencyProperty.Register(
        nameof(BrightPosition),
        typeof(Point),
        typeof(WpfPlotWaferMapBehavior),
        new PropertyMetadata(Point.Empty, PropertyChangedCallback)
    );

    public Point DarkPosition
    {
        get => (Point)GetValue(DarkPositionProperty);
        set => SetValue(DarkPositionProperty, value);
    }

    public static readonly DependencyProperty DarkPositionProperty = DependencyProperty.Register(
        nameof(DarkPosition),
        typeof(Point),
        typeof(WpfPlotWaferMapBehavior),
        new PropertyMetadata(Point.Empty, PropertyChangedCallback)
    );

    public Point StagePosition
    {
        get => (Point)GetValue(StagePositionProperty);
        set => SetValue(StagePositionProperty, value);
    }

    public static readonly DependencyProperty StagePositionProperty = DependencyProperty.Register(
        nameof(StagePosition),
        typeof(Point),
        typeof(WpfPlotWaferMapBehavior),
        new PropertyMetadata(Point.Empty, PropertyChangedCallback)
    );

    public double Theta
    {
        get => (double)GetValue(ThetaProperty);
        set => SetValue(ThetaProperty, value);
    }

    public static readonly DependencyProperty ThetaProperty = DependencyProperty.Register(
        nameof(Theta),
        typeof(double),
        typeof(WpfPlotWaferMapBehavior),
        new PropertyMetadata(0d, PropertyChangedCallback)
    );

    private static void PropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs _)
    {
        if (d is not WpfPlotWaferMapBehavior behaviors || behaviors.AssociatedObject is null) return;

        behaviors.Update();
    }

    #endregion 依赖属性

    private Ellipse? _waferMapCircle;
    private Marker? _positionMarker1;
    private Marker? _positionMarker2;
    private Annotation? _positionAnnotation;

    protected override void OnAttached()
    {
        base.OnAttached();

        AssociatedObject.Plot.DataBackground = new BackgroundStyle { Color = Colors.Transparent };
        AssociatedObject.Plot.FigureBackground = new BackgroundStyle { Color = Colors.White };
        AssociatedObject.UserInputProcessor.IsEnabled = true;
        AssociatedObject.UserInputProcessor.UserActionResponses.Clear();
        AssociatedObject.UserInputProcessor.UserActionResponses.Add(new DoubleClickResponse(StandardMouseButtons.Left, (plotControl, pixel) =>
        {
            var point = plotControl.GetPlotAtPixel(pixel)?.GetCoordinates(pixel);
            if (point is null) return;

            _stageViewModel ??= HostApplication.GetRequiredService<StageViewModel>();

            switch (StageCoordinateSystemEnum)
            {
                case StageCoordinateSystemEnum.Bright:
                    _stageViewModel.SetBrightFieldAbsoluteStageXyByNotAutoFocus(new Point(point.Value.X, point.Value.Y));
                    break;

                case StageCoordinateSystemEnum.Dark:
                    _stageViewModel.SetDarkFieldAbsoluteStageXyByNotAutoFocus(new Point(point.Value.X, point.Value.Y));
                    break;

                case StageCoordinateSystemEnum.Machine:
                    _stageViewModel.SetMachineAbsoluteStageXyByNotAutoFocus(new Point(point.Value.X, point.Value.Y));
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }));

        AssociatedObject.Plot.HideAxesAndGrid();
        AssociatedObject.Plot.Axes.SetLimits(-Radius * 1.1, Radius * 1.1, -Radius * 1.1, Radius * 1.1);

        _waferMapCircle = AssociatedObject.Plot.Add.Circle(0, 0, Radius);
        _waferMapCircle.LineColor = Colors.Transparent;
        _waferMapCircle.FillColor = Color.FromHex("#F0F0F0");

        const float originMaxLengthPixel = 10;

        // 绘制原点
        var originMarker = AssociatedObject.Plot.Add.Marker(0, 0, shape: MarkerShape.Eks, size: originMaxLengthPixel, color: Colors.DarkBlue);
        originMarker.LineWidth = 2;

        _positionMarker1 = AssociatedObject.Plot.Add.Marker(0, 0, shape: MarkerShape.Cross, size: originMaxLengthPixel * 3, color: Colors.Red);
        _positionMarker1.LineWidth = 2;

        _positionMarker2 = AssociatedObject.Plot.Add.Marker(0, 0, shape: MarkerShape.OpenCircle, size: originMaxLengthPixel * 1.5f, color: Colors.Red);
        _positionMarker2.LineWidth = 2;

        // -1130000.333, -1130000.333
        _positionAnnotation = AssociatedObject.Plot.Add.Annotation(string.Empty, alignment: Alignment.UpperLeft);
        _positionAnnotation.LabelBackgroundColor = Colors.Blue.WithAlpha(0.1);
        _positionAnnotation.LabelFontName = Fonts.Monospace;
        _positionAnnotation.LabelFontColor = Colors.Blue;
        _positionAnnotation.LabelBorderWidth = 0;
        _positionAnnotation.LabelBorderColor = Colors.Transparent;
        _positionAnnotation.LabelShadowColor = Colors.Transparent;

        Update();
    }

    private void Update()
    {
        try
        {
            if (_waferMapCircle is not null)
            {
                _waferMapCircle.RadiusX = Radius;
                _waferMapCircle.RadiusY = Radius;
            }

            if (_positionMarker1 is not null)
            {
                _positionMarker1.X = StageCoordinateSystemEnum switch
                {
                    StageCoordinateSystemEnum.Bright => BrightPosition.X,
                    StageCoordinateSystemEnum.Dark => DarkPosition.X,
                    StageCoordinateSystemEnum.Machine => StagePosition.X,
                    _ => throw new ArgumentOutOfRangeException()
                };
                _positionMarker1.Y = StageCoordinateSystemEnum switch
                {
                    StageCoordinateSystemEnum.Bright => BrightPosition.Y,
                    StageCoordinateSystemEnum.Dark => DarkPosition.Y,
                    StageCoordinateSystemEnum.Machine => StagePosition.Y,
                    _ => throw new ArgumentOutOfRangeException()
                };
            }

            if (_positionMarker2 is not null)
            {
                _positionMarker2.X = StageCoordinateSystemEnum switch
                {
                    StageCoordinateSystemEnum.Bright => BrightPosition.X,
                    StageCoordinateSystemEnum.Dark => DarkPosition.X,
                    StageCoordinateSystemEnum.Machine => StagePosition.X,
                    _ => throw new ArgumentOutOfRangeException()
                };
                _positionMarker2.Y = StageCoordinateSystemEnum switch
                {
                    StageCoordinateSystemEnum.Bright => BrightPosition.Y,
                    StageCoordinateSystemEnum.Dark => DarkPosition.Y,
                    StageCoordinateSystemEnum.Machine => StagePosition.Y,
                    _ => throw new ArgumentOutOfRangeException()
                };
            }

            if (_positionAnnotation is not null)
            {
                _positionAnnotation.LabelText = $"""
                                                 Bright(um) : {BrightPosition.ToShortString(),-25}
                                                 Dark(um)   : {DarkPosition.ToShortString(),-25}
                                                 Stage(um)  : {StagePosition.ToShortString(),-25}
                                                 Theta(°)   : {Theta,-25:f4}
                                                 """;
            }
        }
        finally
        {
            AssociatedObject.Refresh();
        }
    }
}
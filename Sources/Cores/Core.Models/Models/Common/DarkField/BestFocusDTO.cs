using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;
using Net.Utilities.ScottPlot.WPF.Extensions;
using Net.Utilities.ScottPlot.WPF.Interfaces;
using Net.Utilities.ScottPlot.WPF.Plottables;
using Net.Utilities.WPF.MVVM;
using ScottPlot;
using ScottPlot.MultiplotLayouts;

namespace Core.Models.Models.Common.DarkField;

public sealed partial class BestFocusDTO : ObservableObject, ICloneable<BestFocusDTO>
{
    [ObservableProperty]
    private string _rawImageFilePath = string.Empty;

    #region X

    [ObservableProperty]
    private IReadOnlyList<Point> _xStrehlRatioPoints = [];

    [ObservableProperty]
    private IReadOnlyList<Point> _xStrehlRatioFitPoints = [];

    [ObservableProperty]
    private IReadOnlyList<IReadOnlyList<Point>> _xStrehlRatioColumnPoints = [];

    [ObservableProperty]
    private Point _bestXStrehlRatioPoint;

    [ObservableProperty]
    private double _bestXStrehlRatioECS;

    [ObservableProperty]
    private IReadOnlyList<IReadOnlyList<Point>> _xIntraRibbonFieldsPoints = [];

    [ObservableProperty]
    private IReadOnlyList<Point> _xFieldTiltPoints = [];

    [ObservableProperty]
    private IReadOnlyList<Point> _xFieldTiltFitPoints = [];

    #endregion

    #region Y

    [ObservableProperty]
    private IReadOnlyList<Point> _yStrehlRatioPoints = [];

    [ObservableProperty]
    private IReadOnlyList<Point> _yStrehlRatioFitPoints = [];

    [ObservableProperty]
    private IReadOnlyList<IReadOnlyList<Point>> _yStrehlRatioColumnPoints = [];

    [ObservableProperty]
    private Point _bestYStrehlRatioPoint;

    [ObservableProperty]
    private double _bestYStrehlRatioECS;

    [ObservableProperty]
    private IReadOnlyList<IReadOnlyList<Point>> _yIntraRibbonFieldsPoints = [];

    [ObservableProperty]
    private IReadOnlyList<Point> _yFieldTiltPoints = [];

    [ObservableProperty]
    private IReadOnlyList<Point> _yFieldTiltFitPoints = [];

    #endregion

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    private IScatterPlotControl _xStrehlRatioScatterPlotControl = HostApplication.GetRequiredService<IScatterPlotControl>();

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    private IScatterPlotControl _yStrehlRatioScatterPlotControl = HostApplication.GetRequiredService<IScatterPlotControl>();

    public BestFocusDTO()
    {
        XStrehlRatioScatterPlotControl.Configure(new Columns(), 3);

        XStrehlRatioScatterPlotControl.SetTitle(0, "X Strehl Ratio(Y: Strehl Ratio - X: px)");
        XStrehlRatioScatterPlotControl.SetTitle(1, "X Intra-Ribbon Fields(Y: Strehl Ratio - X: px)");
        XStrehlRatioScatterPlotControl.SetTitle(2, "X Field Tilt(Y: px - X: Intra-Ribbon)");

        YStrehlRatioScatterPlotControl.Configure(new Columns(), 3);

        YStrehlRatioScatterPlotControl.SetTitle(0, "Y Strehl Ratio(Y: Strehl Ratio - X: px)");
        YStrehlRatioScatterPlotControl.SetTitle(1, "Y Intra-Ribbon Fields(Y: Strehl Ratio - X: px)");
        YStrehlRatioScatterPlotControl.SetTitle(2, "Y Field Tilt(Y: px - X: Intra-Ribbon)");
    }

    // ReSharper disable UnusedParameterInPartialMethod

    partial void OnXStrehlRatioPointsChanged(IReadOnlyList<Point> value) => RefreshPlot();

    partial void OnXStrehlRatioFitPointsChanged(IReadOnlyList<Point> value) => RefreshPlot();

    partial void OnXStrehlRatioColumnPointsChanged(IReadOnlyList<IReadOnlyList<Point>> value) => RefreshPlot();

    partial void OnBestXStrehlRatioPointChanged(Point value) => RefreshPlot();

    partial void OnBestXStrehlRatioECSChanged(double value) => RefreshPlot();

    partial void OnXIntraRibbonFieldsPointsChanged(IReadOnlyList<IReadOnlyList<Point>> value) => RefreshPlot();

    partial void OnXFieldTiltPointsChanged(IReadOnlyList<Point> value) => RefreshPlot();

    partial void OnXFieldTiltFitPointsChanged(IReadOnlyList<Point> value) => RefreshPlot();

    partial void OnYStrehlRatioPointsChanged(IReadOnlyList<Point> value) => RefreshPlot();

    partial void OnYStrehlRatioFitPointsChanged(IReadOnlyList<Point> value) => RefreshPlot();

    partial void OnYStrehlRatioColumnPointsChanged(IReadOnlyList<IReadOnlyList<Point>> value) => RefreshPlot();

    partial void OnBestYStrehlRatioPointChanged(Point value) => RefreshPlot();

    partial void OnBestYStrehlRatioECSChanged(double value) => RefreshPlot();

    partial void OnYIntraRibbonFieldsPointsChanged(IReadOnlyList<IReadOnlyList<Point>> value) => RefreshPlot();

    partial void OnYFieldTiltPointsChanged(IReadOnlyList<Point> value) => RefreshPlot();

    partial void OnYFieldTiltFitPointsChanged(IReadOnlyList<Point> value) => RefreshPlot();

    // ReSharper restore UnusedParameterInPartialMethod

    private void RefreshPlot()
    {
        try
        {
            RefreshBase(XStrehlRatioScatterPlotControl, XStrehlRatioPoints, XStrehlRatioFitPoints, BestXStrehlRatioPoint, BestXStrehlRatioECS);
            Refresh(XStrehlRatioScatterPlotControl, 1, BestXStrehlRatioXPSFPoints, BestXStrehlRatioXPSFFitPoints);
            Refresh(XStrehlRatioScatterPlotControl, 2, BestXStrehlRatioYPSFPoints, BestXStrehlRatioYPSFFitPoints);

            RefreshBase(YStrehlRatioScatterPlotControl, YStrehlRatioPoints, YStrehlRatioFitPoints, BestYStrehlRatioPoint, BestYStrehlRatioECS);
            Refresh(YStrehlRatioScatterPlotControl, 1, BestYStrehlRatioXPSFPoints, BestYStrehlRatioXPSFFitPoints);
            Refresh(YStrehlRatioScatterPlotControl, 2, BestYStrehlRatioYPSFPoints, BestYStrehlRatioYPSFFitPoints);

            RefreshBase(GrayScatterPlotControl, GrayPoints, GrayFitPoints, BestGrayPoint, BestGrayECS);
        }
        finally
        {
            XStrehlRatioScatterPlotControl.AutoScaleRefresh();
            YStrehlRatioScatterPlotControl.AutoScaleRefresh();
            GrayScatterPlotControl.AutoScaleRefresh();

            XStrehlRatioScatterPlotControl.Plot.Axes.SetLimitsY(0.05d, 0.3d);
            YStrehlRatioScatterPlotControl.Plot.Axes.SetLimitsY(0.05d, 0.3d);

            foreach (var plot in XStrehlRatioScatterPlotControl.Multiplot.GetPlots().Skip(1).Concat(YStrehlRatioScatterPlotControl.Multiplot.GetPlots().Skip(1)))
            {
                var fitPoints = plot.GetPlottables().OfType<ScatterLine>().SingleOrDefault()?.ScatterSourcePoints.Points ?? [];

                if (fitPoints.Count > 0)
                {
                    var xes = fitPoints.Select(t => t.X).ToArray();

                    var max = xes.Max();
                    var min = xes.Min();
                    var length = max - min;
                    var middle = (max + min) / 2d;

                    plot.Axes.SetLimitsX(middle - length / 8d, middle + length / 8d);
                }
            }
        }

        return;

        void RefreshBase(IScatterPlotControl scatterPlotControl, IReadOnlyList<Point> points, IReadOnlyList<Point> fitPoints, Point bestPoint, double bestECS)
        {
            var scatterMarkers = scatterPlotControl.GetOrAddScatterMarkerses(0, 2);

            scatterMarkers[0].Update(string.Empty, points, Colors.Gray, MarkerShape.FilledCircle);
            scatterMarkers[1].Update($"Best Strehl: {bestPoint.Y:0.####}, ECS: {bestECS:0.###}", [bestPoint], Colors.Red, MarkerShape.FilledSquare);
            scatterMarkers[1].MarkerSize = 20;

            var scatterLines = scatterPlotControl.GetOrAddScatterLines(0, 1);
            scatterLines[0].Update(string.Empty, fitPoints, Colors.Green);
            scatterLines[0].LineWidth = 2;
            scatterLines[0].MarkerSize = 5;
            scatterLines[0].MarkerColor = Colors.DarkGreen;
        }

        void Refresh(IScatterPlotControl scatterPlotControl, int plotIndex, IReadOnlyList<IReadOnlyList<Point>> points, IReadOnlyList<Point> fitPoints)
        {
            scatterPlotControl.Clear(plotIndex);

            var scatterMarkers = scatterPlotControl.GetOrAddScatterMarkerses(plotIndex, points.Count);

            foreach (var (index, temp) in points.Index())
            {
                scatterMarkers[index].Update(string.Empty, temp, Colors.Gray, MarkerShape.FilledCircle);
            }

            var scatterLines = scatterPlotControl.GetOrAddScatterLines(plotIndex, 1);
            scatterLines[0].Update(string.Empty, fitPoints, Colors.Green);
            scatterLines[0].LineWidth = 2;
            scatterLines[0].MarkerSize = 5;
            scatterLines[0].MarkerColor = Colors.DarkGreen;
        }
    }

    public BestFocusDTO Clone() => new()
    {
        RawImageFilePath = RawImageFilePath,
        XStrehlRatioPoints = [.. XStrehlRatioPoints],
        XStrehlRatioFitPoints = [.. XStrehlRatioFitPoints],
        XStrehlRatioColumnPoints = [.. XStrehlRatioColumnPoints.Select<IReadOnlyList<Point>, IReadOnlyList<Point>>(t => [.. t])],
        BestXStrehlRatioPoint = BestXStrehlRatioPoint,
        BestXStrehlRatioECS = BestXStrehlRatioECS,
        XIntraRibbonFieldsPoints = [.. XIntraRibbonFieldsPoints.Select<IReadOnlyList<Point>, IReadOnlyList<Point>>(t => [.. t])],
        XFieldTiltPoints = [.. XFieldTiltPoints],
        XFieldTiltFitPoints = [.. XFieldTiltFitPoints],
        YStrehlRatioPoints = [.. YStrehlRatioPoints],
        YStrehlRatioFitPoints = [.. YStrehlRatioFitPoints],
        YStrehlRatioColumnPoints = [.. YStrehlRatioColumnPoints.Select<IReadOnlyList<Point>, IReadOnlyList<Point>>(t => [.. t])],
        BestYStrehlRatioPoint = BestYStrehlRatioPoint,
        BestYStrehlRatioECS = BestYStrehlRatioECS,
        YIntraRibbonFieldsPoints = [.. YIntraRibbonFieldsPoints.Select<IReadOnlyList<Point>, IReadOnlyList<Point>>(t => [.. t])],
        YFieldTiltPoints = [.. YFieldTiltPoints],
        YFieldTiltFitPoints = [.. YFieldTiltFitPoints]
    };
}
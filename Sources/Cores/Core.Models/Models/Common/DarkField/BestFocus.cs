using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Algorithms.Modules.CurveFitting;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;
using Net.Utilities.ScottPlot.WPF.Extensions;
using Net.Utilities.ScottPlot.WPF.Helper;
using Net.Utilities.ScottPlot.WPF.Interfaces;
using Net.Utilities.ScottPlot.WPF.Plottables;
using Net.Utilities.WPF.MVVM;
using ScottPlot;
using ScottPlot.MultiplotLayouts;
using Range = ScottPlot.Range;

namespace Core.Models.Models.Common.DarkField;

public sealed partial class BestFocus : ObservableObject, ICloneable<BestFocus>
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
    private double _xFieldTiltFitSlope;

    [ObservableProperty]
    private double _xFieldTiltFitIntercept;

    [ObservableProperty]
    private double _xFieldTiltFitRSquared;

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
    private double _yFieldTiltFitSlope;

    [ObservableProperty]
    private double _yFieldTiltFitIntercept;

    [ObservableProperty]
    private double _yFieldTiltFitRSquared;

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

    public BestFocus()
    {
        XStrehlRatioScatterPlotControl.Configure(new Columns(), 3);

        XStrehlRatioScatterPlotControl.SetTitle(0, "Peek X Strehl Ratio(Y: Strehl Ratio - X: px)");
        XStrehlRatioScatterPlotControl.SetTitle(1, "X Intra-Ribbon Fields(Y: Strehl Ratio - X: px)");
        XStrehlRatioScatterPlotControl.SetTitle(2, "X Field Tilt(Y: px - X: Intra-Ribbon)");
        XStrehlRatioScatterPlotControl.ToggleLegend(1, false);

        YStrehlRatioScatterPlotControl.Configure(new Columns(), 3);

        YStrehlRatioScatterPlotControl.SetTitle(0, "Peek Y Strehl Ratio(Y: Strehl Ratio - X: px)");
        YStrehlRatioScatterPlotControl.SetTitle(1, "Y Intra-Ribbon Fields(Y: Strehl Ratio - X: px)");
        YStrehlRatioScatterPlotControl.SetTitle(2, "Y Field Tilt(Y: px - X: Intra-Ribbon)");
        YStrehlRatioScatterPlotControl.ToggleLegend(1, false);
    }

    // ReSharper disable UnusedParameterInPartialMethod

    partial void OnXStrehlRatioPointsChanged(IReadOnlyList<Point> value) => RefreshXPlot();

    partial void OnXStrehlRatioFitPointsChanged(IReadOnlyList<Point> value) => RefreshXPlot();

    partial void OnXStrehlRatioColumnPointsChanged(IReadOnlyList<IReadOnlyList<Point>> value) => RefreshXPlot();

    partial void OnBestXStrehlRatioPointChanged(Point value) => RefreshXPlot();

    partial void OnBestXStrehlRatioECSChanged(double value) => RefreshXPlot();

    partial void OnXIntraRibbonFieldsPointsChanged(IReadOnlyList<IReadOnlyList<Point>> value) => RefreshXPlot();

    partial void OnXFieldTiltPointsChanged(IReadOnlyList<Point> value) => RefreshXPlot();

    partial void OnXFieldTiltFitSlopeChanged(double value) => RefreshXPlot();

    partial void OnXFieldTiltFitInterceptChanged(double value) => RefreshXPlot();

    partial void OnXFieldTiltFitRSquaredChanged(double value) => RefreshXPlot();

    partial void OnXFieldTiltFitPointsChanged(IReadOnlyList<Point> value) => RefreshXPlot();

    partial void OnYStrehlRatioPointsChanged(IReadOnlyList<Point> value) => RefreshYPlot();

    partial void OnYStrehlRatioFitPointsChanged(IReadOnlyList<Point> value) => RefreshYPlot();

    partial void OnYStrehlRatioColumnPointsChanged(IReadOnlyList<IReadOnlyList<Point>> value) => RefreshYPlot();

    partial void OnBestYStrehlRatioPointChanged(Point value) => RefreshYPlot();

    partial void OnBestYStrehlRatioECSChanged(double value) => RefreshYPlot();

    partial void OnYIntraRibbonFieldsPointsChanged(IReadOnlyList<IReadOnlyList<Point>> value) => RefreshYPlot();

    partial void OnYFieldTiltPointsChanged(IReadOnlyList<Point> value) => RefreshYPlot();

    partial void OnYFieldTiltFitSlopeChanged(double value) => RefreshYPlot();

    partial void OnYFieldTiltFitInterceptChanged(double value) => RefreshYPlot();

    partial void OnYFieldTiltFitRSquaredChanged(double value) => RefreshYPlot();

    partial void OnYFieldTiltFitPointsChanged(IReadOnlyList<Point> value) => RefreshYPlot();

    // ReSharper restore UnusedParameterInPartialMethod

    private void RefreshXPlot() => Refresh(
        XStrehlRatioScatterPlotControl,
        XStrehlRatioPoints,
        XStrehlRatioFitPoints,
        XStrehlRatioColumnPoints,
        BestXStrehlRatioPoint,
        BestXStrehlRatioECS,
        XIntraRibbonFieldsPoints,
        XFieldTiltPoints,
        XFieldTiltFitPoints,
        XFieldTiltFitIntercept,
        XFieldTiltFitSlope,
        XFieldTiltFitRSquared);

    private void RefreshYPlot() => Refresh(
        YStrehlRatioScatterPlotControl,
        YStrehlRatioPoints,
        YStrehlRatioFitPoints,
        YStrehlRatioColumnPoints,
        BestYStrehlRatioPoint,
        BestYStrehlRatioECS,
        YIntraRibbonFieldsPoints,
        YFieldTiltPoints,
        YFieldTiltFitPoints,
        YFieldTiltFitIntercept,
        YFieldTiltFitSlope,
        YFieldTiltFitRSquared);

    private static void Refresh(
        IScatterPlotControl scatterPlotControl,
        IReadOnlyList<Point> strehlRatioPoints,
        IReadOnlyList<Point> strehlRatioFitPoints,
        IReadOnlyList<IReadOnlyList<Point>> strehlRatioColumnPoints,
        Point bestStrehlRatioPoint,
        double bestStrehlRatioECS,
        IReadOnlyList<IReadOnlyList<Point>> intraRibbonFieldsPoints,
        IReadOnlyList<Point> fieldTiltPoints,
        IReadOnlyList<Point> fieldTiltFitPoints,
        double fieldTiltFitSlope,
        double fieldTiltFitIntercept,
        double fieldTiltFitRSquared)
    {
        try
        {
            #region Plot1

            var scatterLines = scatterPlotControl.GetOrAddScatterLines(0, strehlRatioColumnPoints.Count + 1);
            foreach (var (index, temp) in strehlRatioColumnPoints.Index())
            {
                scatterLines[index].Update(string.Empty, temp, Colors.Blue);
                scatterLines[index].MarkerColor = Colors.DarkBlue;
            }

            scatterLines[strehlRatioColumnPoints.Count].Update(string.Empty, strehlRatioFitPoints, Colors.GreenYellow);
            scatterLines[strehlRatioColumnPoints.Count].MarkerColor = Colors.LightGreen;

            if (strehlRatioColumnPoints.Count > 0)
            {
                var scatterMarkers = scatterPlotControl.GetOrAddScatterMarkerses(0, 2);
                scatterMarkers[0].Update(string.Empty, strehlRatioPoints, Colors.DarkBlue, MarkerShape.OpenCircle);
                scatterMarkers[0].MarkerSize = 12;
                scatterMarkers[1].Update(bestStrehlRatioECS > 0 ? $"Best Strehl: {bestStrehlRatioPoint.Y:0.####}, ECS: {bestStrehlRatioECS:0.###}" : $"Best Strehl: {bestStrehlRatioPoint.Y:0.####}", [bestStrehlRatioPoint], Colors.Red, MarkerShape.Asterisk);
                scatterMarkers[1].MarkerSize = 30;
            }

            #endregion

            #region Plot2

            scatterLines = scatterPlotControl.GetOrAddScatterLines(1, intraRibbonFieldsPoints.Count);
            foreach (var (index, temp) in intraRibbonFieldsPoints.Index())
            {
                scatterLines[index].Update($"{index + 1}", temp, Constants.Turbo.GetColor(intraRibbonFieldsPoints.Count - 1 - index, new Range(0, intraRibbonFieldsPoints.Count - 1)));
            }

            #endregion

            #region Plot3

            scatterLines = scatterPlotControl.GetOrAddScatterLines(2, 2);
            scatterLines[0].Update(string.Empty, fieldTiltPoints, Colors.Blue);
            scatterLines[0].MarkerSize = 10;
            scatterLines[0].MarkerColor = Colors.DarkBlue;
            scatterLines[0].MarkerShape = MarkerShape.OpenCircle;

            scatterLines[1].Update(PolynomialCurve.ToString1(fieldTiltFitSlope, fieldTiltFitIntercept, fieldTiltFitRSquared, "0.######"), fieldTiltFitPoints, Colors.Red);

            #endregion
        }
        finally
        {
            scatterPlotControl.AutoScaleRefresh();
        }
    }

    public BestFocus Clone() => new()
    {
        RawImageFilePath = RawImageFilePath,
        XStrehlRatioPoints = [.. XStrehlRatioPoints],
        XStrehlRatioFitPoints = [.. XStrehlRatioFitPoints],
        XStrehlRatioColumnPoints = [.. XStrehlRatioColumnPoints.Select<IReadOnlyList<Point>, IReadOnlyList<Point>>(t => [.. t])],
        BestXStrehlRatioPoint = BestXStrehlRatioPoint,
        BestXStrehlRatioECS = BestXStrehlRatioECS,
        XIntraRibbonFieldsPoints = [.. XIntraRibbonFieldsPoints.Select<IReadOnlyList<Point>, IReadOnlyList<Point>>(t => [.. t])],
        XFieldTiltPoints = [.. XFieldTiltPoints],
        XFieldTiltFitSlope = XFieldTiltFitSlope,
        XFieldTiltFitIntercept = XFieldTiltFitIntercept,
        XFieldTiltFitRSquared = XFieldTiltFitRSquared,
        XFieldTiltFitPoints = [.. XFieldTiltFitPoints],
        YStrehlRatioPoints = [.. YStrehlRatioPoints],
        YStrehlRatioFitPoints = [.. YStrehlRatioFitPoints],
        YStrehlRatioColumnPoints = [.. YStrehlRatioColumnPoints.Select<IReadOnlyList<Point>, IReadOnlyList<Point>>(t => [.. t])],
        BestYStrehlRatioPoint = BestYStrehlRatioPoint,
        BestYStrehlRatioECS = BestYStrehlRatioECS,
        YIntraRibbonFieldsPoints = [.. YIntraRibbonFieldsPoints.Select<IReadOnlyList<Point>, IReadOnlyList<Point>>(t => [.. t])],
        YFieldTiltPoints = [.. YFieldTiltPoints],
        YFieldTiltFitSlope = YFieldTiltFitSlope,
        YFieldTiltFitIntercept = YFieldTiltFitIntercept,
        YFieldTiltFitRSquared = YFieldTiltFitRSquared,
        YFieldTiltFitPoints = [.. YFieldTiltFitPoints]
    };
}
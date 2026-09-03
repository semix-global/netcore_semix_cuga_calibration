using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Algorithms.Modules.CurveFitting;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;
using Net.Utilities.ScottPlot;
using Net.Utilities.ScottPlot.Extensions;
using Net.Utilities.ScottPlot.Interfaces;
using Net.Utilities.ScottPlot.Helper;
using ScottPlot;
using ScottPlot.MultiplotLayouts;
using Range = ScottPlot.Range;

namespace Core.Models.Models.Common.DarkField;

public sealed partial class BestFocus : ObservableObject, ICloneable<BestFocus>
{
    [ObservableProperty]
    public partial bool IsAlgorithmOk { get; set; }

    [ObservableProperty]
    public partial string RawImageFilePath { get; set; } = string.Empty;

    #region X

    [ObservableProperty]
    public partial IReadOnlyList<Point> XStrehlRatioPoints { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<Point> XStrehlRatioFitPoints { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<IReadOnlyList<Point>> XStrehlRatioColumnPoints { get; set; } = [];

    [ObservableProperty]
    public partial Point BestXStrehlRatioPoint { get; set; }

    [ObservableProperty]
    public partial double BestXStrehlRatioECS { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<IReadOnlyList<Point>> XIntraRibbonFieldsPoints { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<Point> XFieldTiltPoints { get; set; } = [];

    [ObservableProperty]
    public partial double XFieldTiltFitSlope { get; set; }

    [ObservableProperty]
    public partial double XFieldTiltFitIntercept { get; set; }

    [ObservableProperty]
    public partial double XFieldTiltFitRSquared { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<Point> XFieldTiltFitPoints { get; set; } = [];

    #endregion

    #region Y

    [ObservableProperty]
    public partial IReadOnlyList<Point> YStrehlRatioPoints { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<Point> YStrehlRatioFitPoints { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<IReadOnlyList<Point>> YStrehlRatioColumnPoints { get; set; } = [];

    [ObservableProperty]
    public partial Point BestYStrehlRatioPoint { get; set; }

    [ObservableProperty]
    public partial double BestYStrehlRatioECS { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<IReadOnlyList<Point>> YIntraRibbonFieldsPoints { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<Point> YFieldTiltPoints { get; set; } = [];

    [ObservableProperty]
    public partial double YFieldTiltFitSlope { get; set; }

    [ObservableProperty]
    public partial double YFieldTiltFitIntercept { get; set; }

    [ObservableProperty]
    public partial double YFieldTiltFitRSquared { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<Point> YFieldTiltFitPoints { get; set; } = [];

    #endregion

    [ObservableProperty]
    public partial double SpotAreaPercentMean { get; set; }

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial IPlotDataSource XStrehlRatioPlotDataSource { get; set; } = new PlotDataSource();

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial IPlotDataSource YStrehlRatioPlotDataSource { get; set; } = new PlotDataSource();

    public BestFocus()
    {
        XStrehlRatioPlotDataSource.Configure(new Columns(), 3);

        XStrehlRatioPlotDataSource.SetTitle(0, "Peek X Strehl Ratio(Y: Strehl Ratio - X: px)");
        XStrehlRatioPlotDataSource.SetTitle(1, "X Intra-Ribbon Fields(Y: Strehl Ratio - X: px)");
        XStrehlRatioPlotDataSource.SetTitle(2, "X Field Tilt(Y: px - X: Intra-Ribbon)");
        XStrehlRatioPlotDataSource.ToggleLegend(1, false);

        YStrehlRatioPlotDataSource.Configure(new Columns(), 3);

        YStrehlRatioPlotDataSource.SetTitle(0, "Peek Y Strehl Ratio(Y: Strehl Ratio - X: px)");
        YStrehlRatioPlotDataSource.SetTitle(1, "Y Intra-Ribbon Fields(Y: Strehl Ratio - X: px)");
        YStrehlRatioPlotDataSource.SetTitle(2, "Y Field Tilt(Y: px - X: Intra-Ribbon)");
        YStrehlRatioPlotDataSource.ToggleLegend(1, false);
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

    partial void OnSpotAreaPercentMeanChanged(double value)
    {
        RefreshXPlot();
        RefreshYPlot();
    }

    partial void OnXStrehlRatioPlotDataSourceChanged(IPlotDataSource value) => RefreshXPlot();

    partial void OnYStrehlRatioPlotDataSourceChanged(IPlotDataSource value) => RefreshYPlot();

    // ReSharper restore UnusedParameterInPartialMethod

    private void RefreshXPlot()
    {
        if (XStrehlRatioPlotDataSource is null) return;

        Refresh(
            XStrehlRatioPlotDataSource,
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
            XFieldTiltFitRSquared,
            SpotAreaPercentMean);
    }

    private void RefreshYPlot()
    {
        if (YStrehlRatioPlotDataSource is null) return;

        Refresh(
            YStrehlRatioPlotDataSource,
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
            YFieldTiltFitRSquared,
            SpotAreaPercentMean);
    }

    private static void Refresh(
        IPlotDataSource plotDataSource,
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
        double fieldTiltFitRSquared,
        double spotAreaPercentMean)
    {
        try
        {
            #region Plot1

            var scatterLines = plotDataSource.GetOrAddScatterLines(0, strehlRatioColumnPoints.Count + 1);
            foreach (var (index, temp) in strehlRatioColumnPoints.Index())
            {
                scatterLines[index].Update(string.Empty, temp, Colors.Blue);
                scatterLines[index].MarkerColor = Colors.DarkBlue;
            }

            scatterLines[strehlRatioColumnPoints.Count].Update(string.Empty, strehlRatioFitPoints, Colors.GreenYellow);
            scatterLines[strehlRatioColumnPoints.Count].MarkerColor = Colors.LightGreen;

            if (strehlRatioColumnPoints.Count > 0)
            {
                var scatterMarkers = plotDataSource.GetOrAddScatterMarkerses(0, 2);
                scatterMarkers[0].Update($"Spot Area Percent: {spotAreaPercentMean:0.####}", strehlRatioPoints, Colors.DarkBlue, MarkerShape.OpenCircle);
                scatterMarkers[0].MarkerSize = 12;
                scatterMarkers[1].Update(bestStrehlRatioECS > 0
                    ? $"Best Strehl: {bestStrehlRatioPoint.Y:0.####}, ECS: {bestStrehlRatioECS:0.###}"
                    : $"Best Strehl: {bestStrehlRatioPoint.Y:0.####}", [bestStrehlRatioPoint], Colors.Red, MarkerShape.Asterisk);
                scatterMarkers[1].MarkerSize = 30;
            }

            #endregion

            #region Plot2

            scatterLines = plotDataSource.GetOrAddScatterLines(1, intraRibbonFieldsPoints.Count);
            foreach (var (index, temp) in intraRibbonFieldsPoints.Index())
            {
                scatterLines[index].Update($"{index + 1}", temp, Constants.Turbo.GetColor(intraRibbonFieldsPoints.Count - 1 - index, new Range(0, intraRibbonFieldsPoints.Count - 1)));
            }

            #endregion

            #region Plot3

            scatterLines = plotDataSource.GetOrAddScatterLines(2, 2);
            scatterLines[0].Update(string.Empty, fieldTiltPoints, Colors.Blue);
            scatterLines[0].MarkerSize = 10;
            scatterLines[0].MarkerColor = Colors.DarkBlue;
            scatterLines[0].MarkerShape = MarkerShape.OpenCircle;

            scatterLines[1].Update(PolynomialCurve.ToString1(fieldTiltFitSlope, fieldTiltFitIntercept, fieldTiltFitRSquared, "0.######"), fieldTiltFitPoints, Colors.Red);

            #endregion
        }
        finally
        {
            plotDataSource.AutoScaleRefresh();
        }
    }

    public BestFocus Clone() => new()
    {
        IsAlgorithmOk = IsAlgorithmOk,
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
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Models.Geometries;
using Net.Utilities.ScottPlot.WPF.Interfaces;
using Net.Utilities.WPF.MVVM;
using ScottPlot;
using ScottPlot.MultiplotLayouts;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public sealed partial class ChirpAODWaveformTrainingItem : ObservableObject, IEquatable<ChirpAODWaveformTrainingItem>
{
    [ObservableProperty]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [ObservableProperty]
    private LaserLightInformation _laserLightInformation = LaserLightInformation.Default;

    [ObservableProperty]
    private CIBInformation _cIBInformation = CIBInformation.Default;

    [ObservableProperty]
    private string _prescanAODWaveformResultFilePath = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<PrescanAODWaveformProfile> _prescanAODWaveformProfiles = [];

    [ObservableProperty]
    private string _chirpAODWaveformResultFilePath = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<ChirpAODWaveformProfile> _chirpAODWaveformProfiles = [];

    [ObservableProperty]
    private double _p3Coefficient;

    [ObservableProperty]
    private double _p4Coefficient;

    [ObservableProperty]
    private double _p5Coefficient;

    [ObservableProperty]
    private double _p6Coefficient;

    [ObservableProperty]
    private double _p7Coefficient;

    [ObservableProperty]
    private double _p8Coefficient;

    [ObservableProperty]
    private string _rawImageFilePath = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<Point> _xStrehlRatioPoints = [];

    [ObservableProperty]
    private IReadOnlyList<Point> _xStrehlRatioFitPoints = [];

    [ObservableProperty]
    private Point _bestXStrehlRatio;

    [ObservableProperty]
    private IReadOnlyList<Point> _yStrehlRatioPoints = [];

    [ObservableProperty]
    private IReadOnlyList<Point> _yStrehlRatioFitPoints = [];

    [ObservableProperty]
    private Point _bestYStrehlRatio;

    [ObservableProperty]
    private IReadOnlyList<Point> _grayPoints = [];

    [ObservableProperty]
    private IReadOnlyList<Point> _grayFitPoints = [];

    [ObservableProperty]
    private Point _bestGray;

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    [property: LiteDB.BsonIgnore]
    private IScatterPlotControl _scatterPlotControl = HostApplication.GetRequiredService<IScatterPlotControl>();

    public ChirpAODWaveformTrainingItem()
    {
        ScatterPlotControl.Configure(new Columns(), 3);

        ScatterPlotControl.SetTitle(0, "X Strehl Ratio(Y: Strehl Ratio - X: px)");
        ScatterPlotControl.SetTitle(1, "Y Strehl Ratio(Y: Strehl Ratio - X: px)");
        ScatterPlotControl.SetTitle(2, "Gray(Y: Gray - X: px)");
    }

    // ReSharper disable UnusedParameterInPartialMethod

    partial void OnXStrehlRatioPointsChanged(IReadOnlyList<Point> value) => RefreshPlot();

    partial void OnXStrehlRatioFitPointsChanged(IReadOnlyList<Point> value) => RefreshPlot();

    partial void OnBestXStrehlRatioChanged(Point value) => RefreshPlot();

    partial void OnYStrehlRatioPointsChanged(IReadOnlyList<Point> value) => RefreshPlot();

    partial void OnYStrehlRatioFitPointsChanged(IReadOnlyList<Point> value) => RefreshPlot();

    partial void OnBestYStrehlRatioChanged(Point value) => RefreshPlot();

    partial void OnGrayPointsChanged(IReadOnlyList<Point> value) => RefreshPlot();

    partial void OnGrayFitPointsChanged(IReadOnlyList<Point> value) => RefreshPlot();

    partial void OnBestGrayChanged(Point value) => RefreshPlot();

    // ReSharper restore UnusedParameterInPartialMethod

    private void RefreshPlot()
    {
        try
        {
            Refresh(0, XStrehlRatioPoints, XStrehlRatioFitPoints, BestXStrehlRatio);
            Refresh(1, YStrehlRatioPoints, YStrehlRatioFitPoints, BestYStrehlRatio);
            Refresh(2, GrayPoints, GrayFitPoints, BestGray);
        }
        finally
        {
            ScatterPlotControl.AutoScaleRefresh();

            foreach (var plot in ScatterPlotControl.Multiplot.GetPlots()) plot.Axes.SetLimitsY(0.1d, 0.3d);
        }

        return;

        void Refresh(int plotIndex, IReadOnlyList<Point> points, IReadOnlyList<Point> fitPoints, Point bestPoint)
        {
            var scatterMarkers = ScatterPlotControl.GetOrAddScatterMarkerses(plotIndex, 2);

            scatterMarkers[0].Update(string.Empty, points, Colors.Gray, MarkerShape.FilledCircle);
            scatterMarkers[1].Update(string.Empty, [bestPoint], Colors.Red, MarkerShape.FilledSquare);
            scatterMarkers[1].MarkerSize = 20;

            var scatterLines = ScatterPlotControl.GetOrAddScatterLines(plotIndex, 1);
            scatterLines[0].Update(string.Empty, fitPoints, Colors.Green);
        }
    }

    public object ToHtmlAnonymous() => new
    {
        ProductivityInformation,
        LaserLightInformation,
        CIBInformation,
        P3Coefficient,
        P4Coefficient,
        P5Coefficient,
        P6Coefficient,
        P7Coefficient,
        P8Coefficient,
        RawImageFilePath,
        BestYStrehlRatio
    };

    public bool Equals(ChirpAODWaveformTrainingItem? other) => ReferenceEquals(this, other) || (P3Coefficient.Equals(other?.P3Coefficient)
                                                                                                && P4Coefficient.Equals(other.P4Coefficient)
                                                                                                && P5Coefficient.Equals(other.P5Coefficient)
                                                                                                && P6Coefficient.Equals(other.P6Coefficient)
                                                                                                && P7Coefficient.Equals(other.P7Coefficient)
                                                                                                && P8Coefficient.Equals(other.P8Coefficient)
                                                                                                && RawImageFilePath.Equals(other.RawImageFilePath));

    public override bool Equals(object? obj) => obj is ChirpAODWaveformTrainingItem other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(P3Coefficient, P4Coefficient, P5Coefficient, P6Coefficient, P7Coefficient, P8Coefficient, RawImageFilePath);
}
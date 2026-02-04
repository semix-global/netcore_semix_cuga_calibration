using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Models.Geometries;
using Net.Utilities.ScottPlot.WPF.Extensions;
using Net.Utilities.ScottPlot.WPF.Helper;
using Net.Utilities.ScottPlot.WPF.Interfaces;
using Net.Utilities.ScottPlot.WPF.Plottables;
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
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    [property: LiteDB.BsonIgnore]
    private IReadOnlyList<Point> _xStrehlRatioPoints = [];

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    [property: LiteDB.BsonIgnore]
    private IReadOnlyList<Point> _xStrehlRatioFitPoints = [];

    [ObservableProperty]
    private Point _bestXStrehlRatioPoint;

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    [property: LiteDB.BsonIgnore]
    private IReadOnlyList<Point> _yStrehlRatioPoints = [];

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    [property: LiteDB.BsonIgnore]
    private IReadOnlyList<Point> _yStrehlRatioFitPoints = [];

    [ObservableProperty]
    private Point _bestYStrehlRatioPoint;

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    [property: LiteDB.BsonIgnore]
    private IReadOnlyList<Point> _grayPoints = [];

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    [property: LiteDB.BsonIgnore]
    private IReadOnlyList<Point> _grayFitPoints = [];

    [ObservableProperty]
    private Point _bestGrayPoint;

    [ObservableProperty]
    private IReadOnlyList<IReadOnlyList<Point>> _bestXStrehlRatioXPSFPoints = [];

    [ObservableProperty]
    private IReadOnlyList<Point> _bestXStrehlRatioXPSFFitPoints = [];

    [ObservableProperty]
    private IReadOnlyList<IReadOnlyList<Point>> _bestXStrehlRatioYPSFPoints = [];

    [ObservableProperty]
    private IReadOnlyList<Point> _bestXStrehlRatioYPSFFitPoints = [];

    [ObservableProperty]
    private IReadOnlyList<IReadOnlyList<Point>> _bestYStrehlRatioXPSFPoints = [];

    [ObservableProperty]
    private IReadOnlyList<Point> _bestYStrehlRatioXPSFFitPoints = [];

    [ObservableProperty]
    private IReadOnlyList<IReadOnlyList<Point>> _bestYStrehlRatioYPSFPoints = [];

    [ObservableProperty]
    private IReadOnlyList<Point> _bestYStrehlRatioYPSFFitPoints = [];

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    [property: LiteDB.BsonIgnore]
    private IScatterPlotControl _xStrehlRatioScatterPlotControl = HostApplication.GetRequiredService<IScatterPlotControl>();

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    [property: LiteDB.BsonIgnore]
    private IScatterPlotControl _yStrehlRatioScatterPlotControl = HostApplication.GetRequiredService<IScatterPlotControl>();

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    [property: LiteDB.BsonIgnore]
    private IScatterPlotControl _grayScatterPlotControl = HostApplication.GetRequiredService<IScatterPlotControl>();

    public ChirpAODWaveformTrainingItem()
    {
        XStrehlRatioScatterPlotControl.Configure(new Columns(), 3);

        XStrehlRatioScatterPlotControl.SetTitle(0, "X Strehl Ratio(Y: Strehl Ratio - X: px)");
        XStrehlRatioScatterPlotControl.SetTitle(1, "X PSF(Y: Gray - X: px)");
        XStrehlRatioScatterPlotControl.SetTitle(2, "Y PSF(Y: Gray - X: px)");

        YStrehlRatioScatterPlotControl.Configure(new Columns(), 3);

        YStrehlRatioScatterPlotControl.SetTitle(0, "Y Strehl Ratio(Y: Strehl Ratio - X: px)");
        YStrehlRatioScatterPlotControl.SetTitle(1, "X PSF(Y: Gray - X: px)");
        YStrehlRatioScatterPlotControl.SetTitle(2, "Y PSF(Y: Gray - X: px)");

        GrayScatterPlotControl.SetTitle("Gray(Y: Gray - X: px)");
    }

    // ReSharper disable UnusedParameterInPartialMethod

    partial void OnXStrehlRatioPointsChanged(IReadOnlyList<Point> value) => RefreshPlot();

    partial void OnXStrehlRatioFitPointsChanged(IReadOnlyList<Point> value) => RefreshPlot();

    partial void OnBestXStrehlRatioPointChanged(Point value) => RefreshPlot();

    partial void OnYStrehlRatioPointsChanged(IReadOnlyList<Point> value) => RefreshPlot();

    partial void OnYStrehlRatioFitPointsChanged(IReadOnlyList<Point> value) => RefreshPlot();

    partial void OnBestYStrehlRatioPointChanged(Point value) => RefreshPlot();

    partial void OnGrayPointsChanged(IReadOnlyList<Point> value) => RefreshPlot();

    partial void OnGrayFitPointsChanged(IReadOnlyList<Point> value) => RefreshPlot();

    partial void OnBestGrayPointChanged(Point value) => RefreshPlot();

    // ReSharper restore UnusedParameterInPartialMethod

    private void RefreshPlot()
    {
        try
        {
            RefreshBase(XStrehlRatioScatterPlotControl, XStrehlRatioPoints, XStrehlRatioFitPoints, BestXStrehlRatioPoint);
            Refresh(XStrehlRatioScatterPlotControl, 1, BestXStrehlRatioXPSFPoints, BestXStrehlRatioXPSFFitPoints);
            Refresh(XStrehlRatioScatterPlotControl, 2, BestXStrehlRatioYPSFPoints, BestXStrehlRatioYPSFFitPoints);

            RefreshBase(YStrehlRatioScatterPlotControl, YStrehlRatioPoints, YStrehlRatioFitPoints, BestYStrehlRatioPoint);
            Refresh(YStrehlRatioScatterPlotControl, 1, BestYStrehlRatioXPSFPoints, BestYStrehlRatioXPSFFitPoints);
            Refresh(YStrehlRatioScatterPlotControl, 2, BestYStrehlRatioYPSFPoints, BestYStrehlRatioYPSFFitPoints);

            RefreshBase(GrayScatterPlotControl, GrayPoints, GrayFitPoints, BestGrayPoint);
        }
        finally
        {
            XStrehlRatioScatterPlotControl.AutoScaleRefresh();
            YStrehlRatioScatterPlotControl.AutoScaleRefresh();
            GrayScatterPlotControl.AutoScaleRefresh();

            XStrehlRatioScatterPlotControl.Plot.Axes.SetLimitsY(0.05d, 0.3d);
            YStrehlRatioScatterPlotControl.Plot.Axes.SetLimitsY(0.05d, 0.3d);
        }

        return;

        void RefreshBase(IScatterPlotControl scatterPlotControl, IReadOnlyList<Point> points, IReadOnlyList<Point> fitPoints, Point bestPoint)
        {
            var scatterMarkers = scatterPlotControl.GetOrAddScatterMarkerses(0, 2);

            scatterMarkers[0].Update(string.Empty, points, Colors.Gray, MarkerShape.FilledCircle);
            scatterMarkers[1].Update(string.Empty, [bestPoint], Colors.Red, MarkerShape.FilledSquare);
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
        BestYStrehlRatioPoint
    };

    public bool Equals(ChirpAODWaveformTrainingItem? other) => ReferenceEquals(this, other) || (P3Coefficient.Equals(other?.P3Coefficient)
                                                                                                && P4Coefficient.Equals(other.P4Coefficient)
                                                                                                && P5Coefficient.Equals(other.P5Coefficient)
                                                                                                && P6Coefficient.Equals(other.P6Coefficient)
                                                                                                && P7Coefficient.Equals(other.P7Coefficient)
                                                                                                && P8Coefficient.Equals(other.P8Coefficient)
                                                                                                && RawImageFilePath.Equals(other.RawImageFilePath)
                                                                                                && BestYStrehlRatioPoint.Equals(other.BestYStrehlRatioPoint));

    public override bool Equals(object? obj) => obj is ChirpAODWaveformTrainingItem other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(P3Coefficient, P4Coefficient, P5Coefficient, P6Coefficient, P7Coefficient, P8Coefficient, RawImageFilePath);
}
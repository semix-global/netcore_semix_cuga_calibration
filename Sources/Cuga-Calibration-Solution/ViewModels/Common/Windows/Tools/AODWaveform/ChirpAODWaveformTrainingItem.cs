using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.Pattern;
using Local.NoSQL.DB.Providers.Bases;
using Net.Utilities.Models.Geometries;
using Net.Utilities.ScottPlot.WPF.Interfaces;
using Net.Utilities.WPF.MVVM;
using ScottPlot;
using ScottPlot.MultiplotLayouts;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public sealed partial class ChirpAODWaveformTrainingItem : ObservableCacheBase
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
    private string _rawImageFilePath = string.Empty;

    [ObservableProperty]
    private string _imageFilePath = string.Empty;

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
            Refresh(0, YStrehlRatioPoints, YStrehlRatioFitPoints, BestYStrehlRatio);
            Refresh(0, GrayPoints, GrayFitPoints, BestGray);
        }
        finally
        {
            ScatterPlotControl.AutoScaleRefresh();
        }

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
}
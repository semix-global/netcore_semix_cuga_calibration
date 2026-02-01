using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.AODWaveform.Generates;
using Core.Models.Models.Common.Pattern;
using Local.NoSQL.DB.Providers.Bases;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public sealed partial class ChirpAODWaveformTrainingCache : ObservableCacheBase
{
    [ObservableProperty]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [ObservableProperty]
    private LaserLightInformation _laserLightInformation = LaserLightInformation.Default;

    [ObservableProperty]
    private GeneratePrescanAODWaveformParam _generatePrescanAODWaveformParam = new();

    [ObservableProperty]
    private GenerateChirpAODWaveformParam _generateChirpAODWaveformParam = new();

    [ObservableProperty]
    private CIBInformation _cIBInformation = CIBInformation.Default;

    [ObservableProperty]
    private CIBConfiguration _cIBConfiguration = new();

    [ObservableProperty]
    private Point _dSWMachinePosition = Point.Origin;

    [ObservableProperty]
    private double _scanLength;

    [ObservableProperty]
    private double _centerECS;

    [ObservableProperty]
    private double _rangeECS;

    [ObservableProperty]
    private bool _isConfirmBestYStrehlRatioResult = true;

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
    private double _startP3Coefficient;

    [ObservableProperty]
    private double _stepP3Coefficient = 0.01;

    [ObservableProperty]
    private double _stopP3Coefficient;

    [ObservableProperty]
    private double _startP4Coefficient;

    [ObservableProperty]
    private double _stepP4Coefficient = 0.01;

    [ObservableProperty]
    private double _stopP4Coefficient;

    [ObservableProperty]
    private double _startP5Coefficient;

    [ObservableProperty]
    private double _stepP5Coefficient = 0.01;

    [ObservableProperty]
    private double _stopP5Coefficient;

    [ObservableProperty]
    private double _startP6Coefficient;

    [ObservableProperty]
    private double _stepP6Coefficient = 0.01;

    [ObservableProperty]
    private double _stopP6Coefficient;

    [ObservableProperty]
    private double _startP7Coefficient;

    [ObservableProperty]
    private double _stepP7Coefficient = 0.01;

    [ObservableProperty]
    private double _stopP7Coefficient;

    [ObservableProperty]
    private double _startP8Coefficient;

    [ObservableProperty]
    private double _stepP8Coefficient = 0.01;

    [ObservableProperty]
    private double _stopP8Coefficient;

    [ObservableProperty]
    private int _retryTimes = 10;

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    [property: LiteDB.BsonIgnore]
    private ChirpAODWaveformTrainingItem _selectedItem = new();

    [ObservableProperty]
    private ChirpAODWaveformTrainingItem _item = new();

    [ObservableProperty]
    private IReadOnlyList<ChirpAODWaveformTrainingItem> _items = [];

    partial void OnItemChanged(ChirpAODWaveformTrainingItem value)
    {
        P3Coefficient = value.P3Coefficient;
        P4Coefficient = value.P4Coefficient;
        P5Coefficient = value.P5Coefficient;
        P6Coefficient = value.P6Coefficient;
        P7Coefficient = value.P7Coefficient;
        P8Coefficient = value.P8Coefficient;
    }

    public object ToHtmlAnonymous() => new
    {
        ProductivityInformation,
        LaserLightInformation,
        GeneratePrescanAODWaveformParam = new HtmlQuote(GeneratePrescanAODWaveformParam.ToHtmlAnonymous()),
        GenerateChirpAODWaveformParam = new HtmlQuote(GenerateChirpAODWaveformParam.ToHtmlAnonymous()),
        CIBInformation = new HtmlQuote(CIBInformation.ToHtmlAnonymous()),
        CIBConfiguration = new HtmlQuote(CIBConfiguration.ToHtmlAnonymous()),
        DSWMachinePosition,
        ScanLength,
        CenterECS,
        RangeECS,
        IsConfirmBestYStrehlRatioResult,
        P3Coefficient,
        P4Coefficient,
        P5Coefficient,
        P6Coefficient,
        P7Coefficient,
        P8Coefficient,
        StartP3Coefficient,
        StepP3Coefficient,
        StopP3Coefficient,
        StartP4Coefficient,
        StepP4Coefficient,
        StopP4Coefficient,
        StartP5Coefficient,
        StepP5Coefficient,
        StopP5Coefficient,
        StartP6Coefficient,
        StepP6Coefficient,
        StopP6Coefficient,
        StartP7Coefficient,
        StepP7Coefficient,
        StopP7Coefficient,
        StartP8Coefficient,
        StepP8Coefficient,
        StopP8Coefficient,
        RetryTimes
    };
}
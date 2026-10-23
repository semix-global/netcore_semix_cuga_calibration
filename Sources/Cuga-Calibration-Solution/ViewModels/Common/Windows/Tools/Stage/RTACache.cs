using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Stage;
using Core.Models.Models;
using Core.Models.Models.Common.Alignment;
using Core.Models.Models.Common.Pattern;
using Newtonsoft.Json;
using System.ComponentModel;
using Point = Net.Utilities.Models.Geometries.Point;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.Stage;

public sealed partial class RTACache : CalibrationCacheBase
{
    private WaferMapState? _waferMapState;

    #region Common

    [ObservableProperty]
    public partial ProductivityInformation ProductivityInformation { get; set; } = ProductivityInformation.Default;

    [ObservableProperty]
    public partial MicroscopeLensInformation MicroscopeLensInformation { get; set; } = MicroscopeLensInformation.Default;

    [ObservableProperty]
    public partial LaserLightInformation LaserLightInformation { get; set; } = LaserLightInformation.Default;

    [ObservableProperty]
    public partial CIBInformation CIBInformation { get; set; } = CIBInformation.Default;

    [ObservableProperty]
    public partial CalChipSiteModelEnum CalChipSiteModelEnum { get; set; } = CalChipSiteModelEnum.ChuckModel;

    [ObservableProperty]
    public partial OpticsConfiguration OpticsConfiguration { get; set; } = new();

    [ObservableProperty]
    public partial CIBConfiguration CIBConfiguration { get; set; } = new();

    #endregion

    #region Step0

    [ObservableProperty]
    public partial bool IsDarkFieldAlignment { get; set; }

    #endregion

    #region Step1

    [ObservableProperty]
    public partial double WaferRadius { get; set; }

    [ObservableProperty]
    public partial double DiePitchWidth { get; set; }

    [ObservableProperty]
    public partial double DiePitchHeight { get; set; }

    [ObservableProperty]
    public partial StageMapTemplate[] StageMapTemplates { get; set; } = [];

    #endregion

    #region Result

    [ObservableProperty]
    public partial AlignmentResultDto AlignmentResult { get; set; } = new();

    [JsonIgnore]
    [ObservableProperty]
    public partial StageMapDocument StageMapDocument { get; set; } = new();

    [ObservableProperty]
    public partial RTAScanResult ScanResult { get; set; } = new();

    [JsonIgnore]
    public bool IsWaferMapCurrent => _waferMapState is { } state && StageMapTemplates.Length == 1 && state == GetWaferMapState();

    #endregion

    public RTACache()
    {
        AlignmentResult.PropertyChanged += OnAlignmentPropertyChanged;
    }

    partial void OnAlignmentResultChanged(AlignmentResultDto oldValue, AlignmentResultDto newValue)
    {
        oldValue.PropertyChanged -= OnAlignmentPropertyChanged;
        newValue.PropertyChanged += OnAlignmentPropertyChanged;
    }

    partial void OnStageMapTemplatesChanged(StageMapTemplate[] oldValue, StageMapTemplate[] newValue)
    {
        foreach (var item in oldValue) item.PropertyChanged -= OnMarkerPropertyChanged;
        foreach (var (index, item) in newValue.Index())
        {
            item.DisplayIndex = index + 1;
            item.PropertyChanged += OnMarkerPropertyChanged;
        }
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.PropertyName is nameof(ProductivityInformation) or nameof(MicroscopeLensInformation) or nameof(CIBInformation)
            or nameof(CalChipSiteModelEnum) or nameof(WaferRadius) or nameof(DiePitchWidth) or nameof(DiePitchHeight)
            or nameof(StageMapTemplates) or nameof(AlignmentResult)) InvalidateWaferMap();
    }

    private void OnMarkerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(StageMapTemplate.FindBFMachinePosition)) InvalidateWaferMap();
    }

    private void OnAlignmentPropertyChanged(object? sender, PropertyChangedEventArgs e) => InvalidateWaferMap();

    public void InvalidateWaferMap()
    {
        _waferMapState = null;
        OnPropertyChanged(nameof(IsWaferMapCurrent));
    }

    public void MarkWaferMapCurrent()
    {
        _waferMapState = GetWaferMapState();
        OnPropertyChanged(nameof(IsWaferMapCurrent));
    }

    private WaferMapState GetWaferMapState() => new(
        ProductivityInformation.Clone(),
        MicroscopeLensInformation.Clone(),
        CIBInformation.Clone(),
        CalChipSiteModelEnum,
        WaferRadius,
        DiePitchWidth,
        DiePitchHeight,
        StageMapTemplates[0].FindBFMachinePosition,
        AlignmentResult.Degrees,
        AlignmentResult.MarkPoint1,
        AlignmentResult.MarkPoint2);

    public object ToHtmlAnonymous() => new
    {
        ProductivityInformation,
        MicroscopeLensInformation,
        LaserLightInformation,
        CIBInformation,
        CalChipSiteModelEnum,
        OpticsConfiguration,
        CIBConfiguration,
        IsDarkFieldAlignment,
        WaferRadius,
        DiePitchWidth,
        DiePitchHeight,
        IsWaferMapCurrent
    };

    private readonly record struct WaferMapState(
        ProductivityInformation ProductivityInformation,
        MicroscopeLensInformation MicroscopeLensInformation,
        CIBInformation CIBInformation,
        CalChipSiteModelEnum CalChipSiteModelEnum,
        double WaferRadius,
        double DiePitchWidth,
        double DiePitchHeight,
        Point MarkerPosition,
        double AlignmentDegrees,
        Point AlignmentMarkPoint1,
        Point AlignmentMarkPoint2);
}
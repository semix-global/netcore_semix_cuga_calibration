using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Stage;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.AODWaveform.Generates;
using Core.Models.Models.Common.Pattern;
using Local.SQL.Cache.Providers.Bases;

namespace CugaCalibration.ViewModels.Common.Windows.Tools;

public partial class GrabbingDarkImageWindowCache : ObservableCacheBase
{
    [ObservableProperty]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [ObservableProperty]
    private StageCoordinateSystemEnum _stageCoordinateSystemEnum = StageCoordinateSystemEnum.Bright;

    [ObservableProperty]
    private int _imageWidth = 800;

    [ObservableProperty]
    private double _scanLength = 5100;

    [ObservableProperty]
    private int _columnCount = 5;

    [ObservableProperty]
    private double _columnWidth = 15300;

    [ObservableProperty]
    private double _centerECS;

    [ObservableProperty]
    private double _rangeECS;

    [ObservableProperty]
    private IReadOnlyList<CIBInformation> _cIBInformations = [];

    [ObservableProperty]
    private CalChipSiteModelEnum _calChipSiteModelEnum = CalChipSiteModelEnum.ChuckModel;

    [ObservableProperty]
    private CIBConfiguration _cIBConfiguration = new();

    [ObservableProperty]
    private LaserLightInformation _laserLightInformation = null!;

    [ObservableProperty]
    private bool _isForward = true;

    [ObservableProperty]
    private bool _isAutoFocus = true;

    [ObservableProperty]
    private double _eCS = 6000;

    [ObservableProperty]
    private bool _isKeepRawImageCIBProfileModeEnum;

    [ObservableProperty]
    private bool _isGenerateAODWaveform;

    [ObservableProperty]
    private GeneratePrescanAODWaveformParam _generatePrescanAODWaveformParam = new();

    [ObservableProperty]
    private string _prescanAODWaveformResultFilePath = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<PrescanAODWaveformProfile> _prescanAODWaveformProfiles = [];

    [ObservableProperty]
    private GenerateChirpAODWaveformParam _generateChirpAODWaveformParam = new();

    [ObservableProperty]
    private string _chirpAODWaveformResultFilePath = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<ChirpAODWaveformProfile> _chirpAODWaveformProfiles = [];
}
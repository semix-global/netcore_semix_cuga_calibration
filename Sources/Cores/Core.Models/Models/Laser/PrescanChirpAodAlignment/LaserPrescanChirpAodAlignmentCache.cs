using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Laser.PrescanChirpAodAlignment;

public sealed partial class LaserPrescanChirpAodAlignmentCache : CalibrationCacheBase
{
    [ObservableProperty]
    private MicroscopeLensInformation _microscopeLensInformation = MicroscopeLensInformation.Default;

    [ObservableProperty]
    private CIBConfiguration _cIBConfiguration = new();

    [ObservableProperty]
    private StageSpeedEnum _xSpeed = StageSpeedEnum.Low;

    [ObservableProperty]
    private int _pmtId = 8;

    [ObservableProperty]
    private int _widthPixel = 800;

    [ObservableProperty]
    private OpticsMagTypeEnum _opticsMagTypeEnum;

    [ObservableProperty]
    private Point _findPosition;

    [ObservableProperty]
    private double _gain;

    [ObservableProperty]
    private double _prescanFlatnessTime = 4300;

    [ObservableProperty]
    private LaserLightInformation _prescanLaserLightInformation = LaserLightInformation.Default;

    [ObservableProperty]
    private int _prescanFrontAndBackMonotonicEndpointTime;

    [ObservableProperty]
    private double _prescanSampleRate = 1064;

    [ObservableProperty]
    private int _prescanZeroNum;

    [ObservableProperty]
    private int _prescanGenerateRetryCount = 1000;

    [ObservableProperty]
    private double _startPrescanCenterFrequency;

    [ObservableProperty]
    private double _endPrescanCenterFrequency;

    [ObservableProperty]
    private double _stepPrescanCenterFrequency;

    [ObservableProperty]
    private double _threshold;
}
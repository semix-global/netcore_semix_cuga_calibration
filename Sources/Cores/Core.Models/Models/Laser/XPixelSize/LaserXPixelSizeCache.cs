using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Recipe.Wafer;
using Core.Models.Enums.Stage;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Laser.XPixelSize;

public sealed partial class LaserXPixelSizeCache : CalibrationCacheBase
{
    [ObservableProperty]
    private MicroscopeLensInformation _microscopeLensInformation =  MicroscopeLensInformation.Default;

    [ObservableProperty]
    private CIBConfiguration _cIBConfiguration = new();

    [ObservableProperty]
    private WaferMaskTypeEnum _waferMaskTypeEnum = WaferMaskTypeEnum.Caliper;

    [ObservableProperty]
    private OpticsMagTypeEnum _opticsMagTypeEnum = OpticsMagTypeEnum.High;

    [ObservableProperty]
    private double _chuckRadius = 150000;

    [ObservableProperty]
    private Point _findTemplatePosition;

    [ObservableProperty]
    private Point _findStartPosition;

    [ObservableProperty]
    private Point _findEndPosition;

    [ObservableProperty]
    private double _dieWidthUm = 15300;

    [ObservableProperty]
    private double _idealUmPerPixel = 0.324;

    [ObservableProperty]
    private double _highMagLowSpeedPixel = 0.33327562632885804;

    [ObservableProperty]
    private double _highMagMiddleSpeedPixel = 0.325;

    [ObservableProperty]
    private double _highMagHighSpeedPixel = 0.326;

    [ObservableProperty]
    private double _middleMagLowSpeedPixel = 0.327;

    [ObservableProperty]
    private double _middleMagMiddleSpeedPixel = 0.328;

    [ObservableProperty]
    private double _middleMagHighSpeedPixel = 0.329;

    [ObservableProperty]
    private double _lowMagLowSpeedPixel = 0.330;

    [ObservableProperty]
    private double _lowMagMiddleSpeedPixel = 0.331;

    [ObservableProperty]
    private double _lowMagHighSpeedPixel = 0.332;

    [ObservableProperty]
    private int _splitWidthPixel = 1000;

    [ObservableProperty]
    private int _splitImageCount = 18;

    [ObservableProperty]
    private double _nccScoreThreshold = 0.9;

    [ObservableProperty]
    private int _columnNumber = 9;

    [ObservableProperty]
    private double _threshold = 20;

    [ObservableProperty]
    private string _templateFilePath = String.Empty;

    [ObservableProperty]
    private string _templateImageFilePath = String.Empty;

    [ObservableProperty]
    private string _highMagLowSpeedTemplateFilePath = String.Empty;

    [ObservableProperty]
    private string _highMagMiddleSpeedTemplateFilePath = String.Empty;

    [ObservableProperty]
    private string _highMagHighSpeedTemplateFilePath = String.Empty;

    [ObservableProperty]
    private string _middleMagLowSpeedTemplateFilePath = String.Empty;

    [ObservableProperty]
    private string _middleMagMiddleSpeedTemplateFilePath = String.Empty;

    [ObservableProperty]
    private string _middleMagHighSpeedTemplateFilePath = String.Empty;

    [ObservableProperty]
    private string _lowMagLowSpeedTemplateFilePath = String.Empty;

    [ObservableProperty]
    private string _lowMagMiddleSpeedTemplateFilePath = String.Empty;

    [ObservableProperty]
    private string _lowMagHighSpeedTemplateFilePath = String.Empty;

    [ObservableProperty]
    private StageSpeedEnum _xStageSpeedEnum = StageSpeedEnum.Low;

    public void GetMagSpeedIdeaXPixelSize(OpticsMagTypeEnum magType, StageSpeedEnum speedType)
    {
        if (magType == OpticsMagTypeEnum.High && speedType == StageSpeedEnum.Low) IdealUmPerPixel = HighMagLowSpeedPixel;
        if (magType == OpticsMagTypeEnum.High && speedType == StageSpeedEnum.Middle) IdealUmPerPixel = HighMagMiddleSpeedPixel;
        if (magType == OpticsMagTypeEnum.High && speedType == StageSpeedEnum.High) IdealUmPerPixel = HighMagHighSpeedPixel;
        if (magType == OpticsMagTypeEnum.Middle && speedType == StageSpeedEnum.Low) IdealUmPerPixel = MiddleMagLowSpeedPixel;
        if (magType == OpticsMagTypeEnum.Middle && speedType == StageSpeedEnum.Middle) IdealUmPerPixel = MiddleMagMiddleSpeedPixel;
        if (magType == OpticsMagTypeEnum.Middle && speedType == StageSpeedEnum.High) IdealUmPerPixel = MiddleMagHighSpeedPixel;
        if (magType == OpticsMagTypeEnum.Low && speedType == StageSpeedEnum.Low) IdealUmPerPixel = LowMagLowSpeedPixel;
        if (magType == OpticsMagTypeEnum.Low && speedType == StageSpeedEnum.Middle) IdealUmPerPixel = LowMagMiddleSpeedPixel;
        if (magType == OpticsMagTypeEnum.Low && speedType == StageSpeedEnum.High) IdealUmPerPixel = LowMagHighSpeedPixel;
    }

    public void GetMagSpeedTemplateFilePath(OpticsMagTypeEnum magType, StageSpeedEnum speedType)
    {
        if (magType == OpticsMagTypeEnum.High && speedType == StageSpeedEnum.Low) TemplateFilePath = HighMagLowSpeedTemplateFilePath;
        if (magType == OpticsMagTypeEnum.High && speedType == StageSpeedEnum.Middle) TemplateFilePath = HighMagMiddleSpeedTemplateFilePath;
        if (magType == OpticsMagTypeEnum.High && speedType == StageSpeedEnum.High) TemplateFilePath = HighMagHighSpeedTemplateFilePath;
        if (magType == OpticsMagTypeEnum.Middle && speedType == StageSpeedEnum.Low) TemplateFilePath = MiddleMagLowSpeedTemplateFilePath;
        if (magType == OpticsMagTypeEnum.Middle && speedType == StageSpeedEnum.Middle) TemplateFilePath = MiddleMagMiddleSpeedTemplateFilePath;
        if (magType == OpticsMagTypeEnum.Middle && speedType == StageSpeedEnum.High) TemplateFilePath = MiddleMagHighSpeedTemplateFilePath;
        if (magType == OpticsMagTypeEnum.Low && speedType == StageSpeedEnum.Low) TemplateFilePath = LowMagLowSpeedTemplateFilePath;
        if (magType == OpticsMagTypeEnum.Low && speedType == StageSpeedEnum.Middle) TemplateFilePath = LowMagMiddleSpeedTemplateFilePath;
        if (magType == OpticsMagTypeEnum.Low && speedType == StageSpeedEnum.High) TemplateFilePath = LowMagHighSpeedTemplateFilePath;
    }

    public void SetMagSpeedTemplateFilePath(OpticsMagTypeEnum magType, StageSpeedEnum speedType, string filePath)
    {
        if (magType == OpticsMagTypeEnum.High && speedType == StageSpeedEnum.Low) HighMagLowSpeedTemplateFilePath = filePath;
        if (magType == OpticsMagTypeEnum.High && speedType == StageSpeedEnum.Middle) HighMagMiddleSpeedTemplateFilePath = filePath;
        if (magType == OpticsMagTypeEnum.High && speedType == StageSpeedEnum.High) HighMagHighSpeedTemplateFilePath = filePath;
        if (magType == OpticsMagTypeEnum.Middle && speedType == StageSpeedEnum.Low) MiddleMagLowSpeedTemplateFilePath = filePath;
        if (magType == OpticsMagTypeEnum.Middle && speedType == StageSpeedEnum.Middle) MiddleMagMiddleSpeedTemplateFilePath = filePath;
        if (magType == OpticsMagTypeEnum.Middle && speedType == StageSpeedEnum.High) MiddleMagHighSpeedTemplateFilePath = filePath;
        if (magType == OpticsMagTypeEnum.Low && speedType == StageSpeedEnum.Low) LowMagLowSpeedTemplateFilePath = filePath;
        if (magType == OpticsMagTypeEnum.Low && speedType == StageSpeedEnum.Middle) LowMagMiddleSpeedTemplateFilePath = filePath;
        if (magType == OpticsMagTypeEnum.Low && speedType == StageSpeedEnum.High) LowMagHighSpeedTemplateFilePath = filePath;
    }
}
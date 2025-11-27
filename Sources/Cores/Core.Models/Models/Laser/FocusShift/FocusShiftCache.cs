using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Recipe.Wafer;
using Core.Models.Enums.Stage;
using Core.Models.Helper;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Setting;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Laser.FocusShift;

public sealed partial class FocusShiftCache : CalibrationCacheBase
{
    [ObservableProperty]
    private CIBConfiguration _cIBConfiguration = new();

    [ObservableProperty]
    private LaserLightInformation _laserLightInformation = LaserLightInformation.Default;

    [ObservableProperty]
    private OpticsIncidentModeEnum _opticsIncidentModeEnum = CalibrationConstantsHelper.MainOpticsIncidentModeEnum;

    /// <summary>
    /// 根据ecs变化值调节afMotor的系数
    /// </summary>
    [ObservableProperty]
    private double _afEcsRelation = 30;

    [ObservableProperty]
    private SettingDarkFieldAutoFocusParam _lowMagDarkFieldAutoFocusParam = new();

    [ObservableProperty]
    private SettingDarkFieldAutoFocusParam _middleMagDarkFieldAutoFocusParam = new();

    [ObservableProperty]
    private SettingDarkFieldAutoFocusParam _highMagDarkFieldAutoFocusParam = new();

    [ObservableProperty]
    private MicroscopeLensInformation _lowMicroscopeLensInformation = MicroscopeLensInformation.Default;

    [ObservableProperty]
    private MicroscopeLensInformation _highMicroscopeLensInformation = MicroscopeLensInformation.Default;

    [ObservableProperty]
    private WaferMaskTypeEnum _waferMaskTypeEnum = WaferMaskTypeEnum.Undefined;

    [ObservableProperty]
    private OpticsMagTypeEnum _opticsMagTypeEnum = OpticsMagTypeEnum.High;

    [ObservableProperty]
    private StageSpeedEnum _stageSpeedEnum = StageSpeedEnum.Low;

    [ObservableProperty]
    private CalChipSiteModelEnum _calChipSiteModelEnum = CalChipSiteModelEnum.DswModel;

    [ObservableProperty]
    private double _lowMagFindFocusMin;

    [ObservableProperty]
    private double _middleMagFindFocusMin;

    [ObservableProperty]
    private double _highMagFindFocusMin;

    [ObservableProperty]
    private double _lowMagFindFocusMax;

    [ObservableProperty]
    private double _middleMagFindFocusMax;

    [ObservableProperty]
    private double _highMagFindFocusMax;

    [ObservableProperty]
    private double _lowMagFindFocusInterval;

    [ObservableProperty]
    private double _middleMagFindFocusInterval;

    [ObservableProperty]
    private double _highMagFindFocusInterval;

    [ObservableProperty]
    private Point _lowSiteFindPosition;

    [ObservableProperty]
    private Point _highSiteFindPosition;

    [ObservableProperty]
    private Point _darkFieldFindPosition;

    [ObservableProperty]
    private string _lowSiteTemplateFilePath = string.Empty;

    [ObservableProperty]
    private string _lowSiteTemplateImageFilePath = string.Empty;

    [ObservableProperty]
    private string _highSiteTemplateFilePath = string.Empty;

    [ObservableProperty]
    private string _highSiteTemplateImageFilePath = string.Empty;

    [ObservableProperty]
    private string _darkFiledTemplateFilePath = string.Empty;

    [ObservableProperty]
    private string _darkFiledTemplateImageFilePath = string.Empty;

    /// <summary>
    /// 调节AfMotor的阈值(ecs)，小于时不调节
    /// </summary>
    [ObservableProperty]
    private double _afMotorReviseThreshold = 5;

    /// <summary>
    /// af聚焦参数补偿后，af和df焦点差值小于阈值时成功
    /// </summary>
    [ObservableProperty]
    private double _focusShiftThreshold;

    public void SetDarkFieldAutoFocusParam(SettingDarkFieldAutoFocusParam param)
    {
        switch (OpticsMagTypeEnum)
        {
            case OpticsMagTypeEnum.Low:
                LowMagDarkFieldAutoFocusParam = param.Clone();
                break;

            case OpticsMagTypeEnum.Middle:
                MiddleMagDarkFieldAutoFocusParam = param.Clone();
                break;

            case OpticsMagTypeEnum.High:
                HighMagDarkFieldAutoFocusParam = param.Clone();
                break;

            default:
                throw new NotImplementedException();
        }
    }

    public SettingDarkFieldAutoFocusParam GetDarkFieldAutoFocusParam()
        => OpticsMagTypeEnum switch
        {
            OpticsMagTypeEnum.Low => LowMagDarkFieldAutoFocusParam.Clone(),
            OpticsMagTypeEnum.Middle => MiddleMagDarkFieldAutoFocusParam.Clone(),
            OpticsMagTypeEnum.High => HighMagDarkFieldAutoFocusParam.Clone(),
            _ => throw new NotImplementedException()
        };

    public (double min, double max, double interval) GetSteppingRangeParam()
        => OpticsMagTypeEnum switch
        {
            OpticsMagTypeEnum.Low => (LowMagFindFocusMin, LowMagFindFocusMax, LowMagFindFocusInterval),
            OpticsMagTypeEnum.Middle => (MiddleMagFindFocusMin, MiddleMagFindFocusMax, MiddleMagFindFocusInterval),
            OpticsMagTypeEnum.High => (HighMagFindFocusMin, HighMagFindFocusMax, HighMagFindFocusInterval),
            _ => throw new NotImplementedException()
        };
}
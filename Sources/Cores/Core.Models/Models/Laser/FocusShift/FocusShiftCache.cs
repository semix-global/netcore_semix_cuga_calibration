using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Microscope;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Models.Setting;
using Net.Utilities.Models;

namespace Core.Models.Models.Laser.FocusShift;

public sealed partial class FocusShiftCache : CalibrationCacheBase
{
    /// <summary>
    /// todo: 诊断临时用，校准取消，读接口获取
    /// </summary>
    [ObservableProperty]
    private SettingDarkFieldAutoFocusParam _settingDarkFieldAutoFocusParam = new();

    [ObservableProperty]
    private MicroscopeMagnificationEnum _lowMicroscopeMagnificationEnum = MicroscopeMagnificationEnum.Magnification5X;

    [ObservableProperty]
    private MicroscopeMagnificationEnum _highMicroscopeMagnificationEnum = MicroscopeMagnificationEnum.Magnification50X;

    [ObservableProperty]
    private OpticsMagTypeEnum _opticsMagTypeEnum = OpticsMagTypeEnum.High;

    [ObservableProperty]
    private StageSpeedEnum _stageSpeedEnum = StageSpeedEnum.Low;

    [ObservableProperty]
    private CalChipSiteModelEnum _calChipSiteModelEnum = CalChipSiteModelEnum.DswModel;

    [ObservableProperty]
    private double _findFocusMin;

    [ObservableProperty]
    private double _findFocusMax;

    [ObservableProperty]
    private double _findFocusInterval;

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

    [ObservableProperty]
    private double _qualityThreshold;
}

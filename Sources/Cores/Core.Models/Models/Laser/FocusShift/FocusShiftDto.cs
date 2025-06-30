using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Models.Setting;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Laser.FocusShift;

public sealed partial class FocusShiftDto : CalibrationDtoBase, ICloneable<FocusShiftDto>
{
    [ObservableProperty]
    private OpticsMagTypeEnum _opticsMagTypeEnum = OpticsMagTypeEnum.High;

    [ObservableProperty]
    private int _index;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BrightFiedlToDarkFieldOffset))]
    private Point _brightFieldFindPosition;

    [NotifyPropertyChangedFor(nameof(BrightFiedlToDarkFieldOffset))]
    [ObservableProperty]
    private Point _darkFieldFindPosition;

    [ObservableProperty]
    private SettingDarkFieldAutoFocusParam _settingDarkFieldAutoFocusParam = new();

    [ObservableProperty]
    private double _brightFieldEcsValue;

    [NotifyPropertyChangedFor(nameof(FocusShiftOffset))]
    [ObservableProperty]
    private double _darkFieldEcsValue;

    [ObservableProperty]
    private double _nscValue;

    [NotifyPropertyChangedFor(nameof(FocusShiftOffset))]
    [ObservableProperty]
    private double _autoFocusEcs;

    [ObservableProperty]
    private double _autoFocusNsc;

    [ObservableProperty]
    private double _darkFieldQuality;

    [ObservableProperty]
    private string _darkFieldImageFilePath = string.Empty;

    [ObservableProperty]
    private string _darkFieldOriginImageFilePath = string.Empty;

    [ObservableProperty]
    private double _afMotorOffset;

    /// <summary>
    /// 明暗场自动聚焦deltaZ
    /// </summary>
    [ObservableProperty]
    private double _ecsOffset;

    /// <summary>
    /// 照明、采集焦点差值
    /// </summary>
    public double FocusShiftOffset => DarkFieldEcsValue - AutoFocusEcs;

    public Point BrightFiedlToDarkFieldOffset => DarkFieldFindPosition - (Vector)BrightFieldFindPosition;


    public FocusShiftDto Clone() => new()
    {
        OpticsMagTypeEnum = OpticsMagTypeEnum,
        Index = Index,
        BrightFieldFindPosition = BrightFieldFindPosition,
        DarkFieldFindPosition = DarkFieldFindPosition,
        BrightFieldEcsValue = BrightFieldEcsValue,
        DarkFieldEcsValue = DarkFieldEcsValue,
        NscValue = NscValue,
        AutoFocusEcs = AutoFocusEcs,
        AutoFocusNsc = AutoFocusNsc,
        DarkFieldQuality = DarkFieldQuality,
        DarkFieldImageFilePath = DarkFieldImageFilePath,
        DarkFieldOriginImageFilePath = DarkFieldOriginImageFilePath,
        EcsOffset = EcsOffset,
        SettingDarkFieldAutoFocusParam = SettingDarkFieldAutoFocusParam.Clone(),
        AfMotorOffset = AfMotorOffset,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };
}
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Setting;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models;

namespace Core.Models.Models.Laser.FocusShift;

public sealed partial class FocusShiftDto : CalibrationDtoBase, ICloneable<FocusShiftDto>
{
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

    [ObservableProperty]
    private double _darkFieldEcsValue;

    [ObservableProperty]
    private double _darkFieldQuality;

    [ObservableProperty]
    private string _darkFieldImageFilePath = string.Empty;

    [ObservableProperty]
    private string _darkFieldOriginImageFilePath = string.Empty;

    public Point BrightFiedlToDarkFieldOffset => DarkFieldFindPosition - BrightFieldFindPosition;

    [ObservableProperty]
    private double _ecsOffset;

    [ObservableProperty]
    private double _nscValue;

    public FocusShiftDto Clone() => new()
    {
        Index = Index,
        BrightFieldFindPosition = BrightFieldFindPosition,
        DarkFieldFindPosition = DarkFieldFindPosition,
        BrightFieldEcsValue = BrightFieldEcsValue,
        DarkFieldEcsValue = DarkFieldEcsValue,
        DarkFieldQuality = DarkFieldQuality,
        DarkFieldImageFilePath = DarkFieldImageFilePath,
        DarkFieldOriginImageFilePath = DarkFieldOriginImageFilePath,
        EcsOffset = EcsOffset,
        SettingDarkFieldAutoFocusParam = SettingDarkFieldAutoFocusParam.Clone(),
        NscValue = NscValue,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };
}
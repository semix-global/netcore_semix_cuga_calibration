using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.CIB;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Common.Pattern;

public sealed partial class CIBConfiguration : ObservableObject, ICloneable<CIBConfiguration>
{
    [ObservableProperty]
    public partial int Gain { get; set; } = -2;

    [ObservableProperty]
    public partial bool IsAutoGainControl { get; set; } = true;

    [ObservableProperty]
    public partial bool IsL0K { get; set; }

    [ObservableProperty]
    public partial CIBProfileModeEnum CIBProfileMode { get; set; } = CIBProfileModeEnum.PMTLog;

    [ObservableProperty]
    public partial bool IsKeepRawImageCIBProfileModeEnum { get; set; } = false;

    partial void OnCIBProfileModeChanged(CIBProfileModeEnum value)
    {
        if (value != CIBProfileModeEnum.PMTLog) IsKeepRawImageCIBProfileModeEnum = false;
    }

    public CIBConfiguration Clone() => new()
    {
        Gain = Gain,
        IsAutoGainControl = IsAutoGainControl,
        IsL0K = IsL0K,
        CIBProfileMode = CIBProfileMode,
        IsKeepRawImageCIBProfileModeEnum = IsKeepRawImageCIBProfileModeEnum
    };

    public object ToHtmlAnonymous() => new
    {
        Gain,
        IsAutoGainControl,
        IsL0K,
        CIBProfileMode,
        IsKeepRawImageCIBProfileModeEnum
    };
}
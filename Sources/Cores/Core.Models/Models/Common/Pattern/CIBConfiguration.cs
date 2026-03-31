using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.CIB;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Common.Pattern;

public sealed partial class CIBConfiguration : ObservableObject, ICloneable<CIBConfiguration>, IAdaptIn<CIBConfiguration, CIBConfiguration>
{
    [ObservableProperty]
    private int _gain = -2;

    [ObservableProperty]
    private bool _isAutoGainControl = true;

    [ObservableProperty]
    private bool _isL0K;

    [ObservableProperty]
    private CIBProfileModeEnum _cIBProfileMode = CIBProfileModeEnum.PMTLog;

    public CIBConfiguration AdaptIn(CIBConfiguration obj)
    {
        Gain = obj.Gain;
        IsAutoGainControl = obj.IsAutoGainControl;
        IsL0K = obj.IsL0K;
        CIBProfileMode = obj.CIBProfileMode;

        return this;
    }

    public CIBConfiguration Clone() => new()
    {
        Gain = Gain,
        IsAutoGainControl = IsAutoGainControl,
        IsL0K = IsL0K,
        CIBProfileMode = CIBProfileMode
    };

    public object ToHtmlAnonymous() => new
    {
        Gain,
        IsAutoGainControl,
        IsL0K,
        CIBProfileMode
    };
}
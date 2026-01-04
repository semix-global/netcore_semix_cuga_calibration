using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.CIB;
using Local.NoSQL.DB.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Common.Pattern;

public sealed partial class CIBConfiguration : ObservableCacheBase, ICloneable<CIBConfiguration>
{
    [ObservableProperty]
    private int _gain = -2;

    [ObservableProperty]
    private bool _isAutoGainControl = true;

    [ObservableProperty]
    private bool _isL0K;

    [ObservableProperty]
    private CIBProfileModeEnum _cIBProfileMode = CIBProfileModeEnum.PMTLog;

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
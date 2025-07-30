using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.CIB;
using Local.NoSQL.DB.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Pattern
{
    public sealed partial class CIBConfiguration : ObservableCacheBase, ICloneable<CIBConfiguration>
    {
        [ObservableProperty]
        private int _dcGainVoltage = 0;

        [ObservableProperty]
        private bool _isAutoGain = true;

        [ObservableProperty]
        private bool _isL0k = false;

        [ObservableProperty]
        private CIBProfileModeEnum _cIBProfileMode = CIBProfileModeEnum.PMTVoltage;

        public CIBConfiguration Clone() => new()
        {
            DcGainVoltage = DcGainVoltage,
            IsAutoGain = IsAutoGain,
            IsL0k = IsL0k,
            CIBProfileMode = CIBProfileMode
        };
    }
}

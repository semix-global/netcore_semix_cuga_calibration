using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Pattern;
using Cuga.Data.DataStruct.PMT;
using Local.NoSQL.DB.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Common.DarkField;

public sealed partial class CIBDelayDTO : ObservableCacheBase, ICloneable<CIBDelayDTO>, IAdaptTo<CgPMTDelayModel>, IAdaptIn<CgPMTDelayModel, CIBDelayDTO>
{
    [ObservableProperty]
    private CIBInformation _cIBInformation = CIBInformation.Default;

    [ObservableProperty]
    private int _pMTDelay;

    [ObservableProperty]
    private int _senseDelay;

    [ObservableProperty]
    private double _aGCDelay;

    #region Mapper

    public CIBDelayDTO Clone() => new()
    {
        CIBInformation = CIBInformation.Clone(),
        PMTDelay = PMTDelay,
        SenseDelay = SenseDelay,
        AGCDelay = AGCDelay,
        Id = Id,
        Expiration = Expiration
    };

    public CgPMTDelayModel AdaptTo() => new()
    {
        PMTId = CIBInformation.PMTId,
        Channel = CIBInformation.ChannelId,
        PMTDelay = PMTDelay,
        SenseDelay = SenseDelay,
        DAC_Delay = (int)AGCDelay
    };

    public CIBDelayDTO AdaptIn(CgPMTDelayModel obj)
    {
        CIBInformation = CIBInformation.Default.Clone().AdaptIn((obj.PMTId, obj.Channel, true));
        PMTDelay = obj.PMTDelay;
        SenseDelay = obj.SenseDelay;
        AGCDelay = obj.DAC_Delay;

        return this;
    }

    #endregion Mapper
}
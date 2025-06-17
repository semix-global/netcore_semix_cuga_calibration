using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Cuga.Data.DataStruct.PMT;
using Local.NoSQL.DB.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Common.DarkField;

public sealed partial class DarkFieldPmtDelayDto : ObservableCacheBase, ICloneable<DarkFieldPmtDelayDto>, IAdaptTo<CgPMTDelayModel>, IAdaptIn<CgPMTDelayModel, DarkFieldPmtDelayDto>
{
    /// <summary>
    /// PMT ID
    /// </summary>
    [ObservableProperty]
    private int _pmtId;

    /// <summary>
    /// 通道 Id
    /// </summary>
    [ObservableProperty]
    private int _channelId;

    /// <summary>
    /// CH值
    /// </summary>
    [ObservableProperty]
    private int _pmtDelay;

    /// <summary>
    /// CH值
    /// </summary>
    [ObservableProperty]
    private int _senseDelay;

    /// <summary>
    /// CH值
    /// </summary>
    [ObservableProperty]
    private int _dacDelay;

    #region Mapper

    public DarkFieldPmtDelayDto Clone() => new()
    {
        PmtId = PmtId,
        ChannelId = ChannelId,
        PmtDelay = PmtDelay,
        SenseDelay = SenseDelay,
        DacDelay = DacDelay,
        Id = Id,
        Expiration = Expiration
    };

    public CgPMTDelayModel AdaptTo() => new()
    {
        PMTId = PmtId,
        Channel = ChannelId,
        PMTDelay = PmtDelay,
        SenseDelay = SenseDelay,
        DAC_Delay = DacDelay
    };

    public DarkFieldPmtDelayDto AdaptIn(CgPMTDelayModel obj)
    {
        Guard.IsNotNull(obj, nameof(obj));

        PmtId = obj.PMTId;
        ChannelId = obj.Channel;
        PmtDelay = obj.PMTDelay;
        SenseDelay = obj.SenseDelay;
        DacDelay = obj.DAC_Delay;

        return this;
    }

    #endregion Mapper
}
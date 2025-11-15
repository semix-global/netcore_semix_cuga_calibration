using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Local.NoSQL.DB.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;

#if NET
using CgPMTDataModel = Cuga.Data.DataStruct.PMT.CgPMTdataModel;
#else
using Cuga.Data.DataStruct.PMT;

#endif

namespace Core.Models.Models.Common.DarkField;

public sealed partial class DarkFieldPmtDataDto : ObservableCacheBase, ICloneable<DarkFieldPmtDataDto>, IAdaptTo<CgPMTDataModel>, IAdaptIn<CgPMTDataModel, DarkFieldPmtDataDto>
{
    [ObservableProperty]
    private int _pmtId;

    [ObservableProperty]
    private int _channel;

    [ObservableProperty]
    private int _lineCount;

    [ObservableProperty]
    private double _avgData;

    [ObservableProperty]
    private List<double> _data = [];

    #region Mapper

    public DarkFieldPmtDataDto Clone() => new()
    {
        PmtId = PmtId,
        Channel = Channel,
        LineCount = LineCount,
        AvgData = AvgData,
        Data = [.. Data],
        Id = Id,
        Expiration = Expiration
    };

    public CgPMTDataModel AdaptTo() => new()
    {
        PMTId = PmtId,
        Channel = Channel,
        LineCount = LineCount,
        Data = [.. Data],
        AVGData = AvgData
    };

    public DarkFieldPmtDataDto AdaptIn(CgPMTDataModel obj)
    {
        Guard.IsNotNull(obj);

        PmtId = obj.PMTId;
        Channel = obj.Channel;
        LineCount = obj.LineCount;
        Data = obj.Data is not null ? [.. obj.Data] : [];
        AvgData = obj.AVGData;

        return this;
    }

    #endregion Mapper
}
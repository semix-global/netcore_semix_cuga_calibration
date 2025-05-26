using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Cuga.Data.DataStruct.PMT;
using Local.NoSQL.DB.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Common.DarkField;

public sealed partial class DarkFieldPmtDataDto : ObservableCacheBase, ICloneable<DarkFieldPmtDataDto>, IAdaptTo<CgPMTdataModel>, IAdaptIn<CgPMTdataModel, DarkFieldPmtDataDto>
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

    public CgPMTdataModel AdaptTo() => new()
    {
        PMTId = PmtId,
        Channel = Channel,
        LineCount = LineCount,
        Data = [.. Data],
        AVGData = AvgData
    };

    public DarkFieldPmtDataDto AdaptIn(CgPMTdataModel obj)
    {
        Guard.IsNotNull(obj, nameof(obj));

        PmtId = obj.PMTId;
        Channel = obj.Channel;
        LineCount = obj.LineCount;
        Data = obj.Data is not null ? [.. obj.Data] : [];
        AvgData = obj.AVGData;

        return this;
    }

    #endregion Mapper
}
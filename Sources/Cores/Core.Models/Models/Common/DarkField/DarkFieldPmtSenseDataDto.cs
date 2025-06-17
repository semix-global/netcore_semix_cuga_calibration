using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Common.DarkField;

public sealed partial class DarkFieldPmtSenseDataDto : ObservableObject, ICloneable<DarkFieldPmtSenseDataDto>
#if NETFRAMEWORK
    , IAdaptIn<Cuga.Data.DataStruct.PMT.CgPMTSenseModel, DarkFieldPmtSenseDataDto>
#endif
{
    [ObservableProperty]
    private int _pmtId;

    [ObservableProperty]
    private int _channel;

    [ObservableProperty]
    private int _lineCount;

    [ObservableProperty]
    private List<List<double>> _dataList = [];

    [ObservableProperty]
    private List<double> _averageData = [];

    #region Mapper

    public DarkFieldPmtSenseDataDto Clone() => new()
    {
        PmtId = PmtId,
        Channel = Channel,
        LineCount = LineCount,
        DataList =
        [
            .. DataList.Select<List<double>, List<double>>(t =>
            [
                .. t
            ])
        ],
        AverageData = [.. AverageData],
    };

#if NETFRAMEWORK
    public DarkFieldPmtSenseDataDto AdaptIn(Cuga.Data.DataStruct.PMT.CgPMTSenseModel obj)
    {
        CommunityToolkit.Diagnostics.Guard.IsNotNull(obj, nameof(obj));

        PmtId = obj.PMTId;
        Channel = obj.Channel;
        LineCount = obj.LineCount;
        DataList = obj.Data is not null
            ?
            [
                .. obj.Data.Select<List<double>, List<double>>
                    (t => [.. t])
            ]
            : [];
        AverageData = obj.AVGData is not null ? [.. obj.AVGData] : [];

        return this;
    }

#endif

    #endregion Mapper
}
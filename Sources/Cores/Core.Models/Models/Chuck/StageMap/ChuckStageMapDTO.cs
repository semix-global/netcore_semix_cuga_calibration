using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.StageMap;
using Local.SQL.Cache.Providers.Bases;
using StageMapResult = Core.Models.Models.Common.StageMap.StageMap;

namespace Core.Models.Models.Chuck.StageMap;

[CacheVersion("2.0.0")]
public sealed partial class ChuckStageMapDTO : CalibrationDTOBase<ChuckStageMapDTO> /*, IAdaptTo<CalibrationChuckStageMap>*/
{
    [Newtonsoft.Json.JsonIgnore]
    [ObservableProperty]
    public partial StageMapDocument StageMapDocument { get; set; } = new();

    [ObservableProperty]
    public partial StageMapResult BFStageMap { get; set; } = new();

    [ObservableProperty]
    public partial StageMapResult DFStageMap { get; set; } = new();

    [ObservableProperty]
    public partial StageMapResult[] DFRepeatStageMaps { get; set; } = [];

    [ObservableProperty]
    public partial StageMapResult DownloadStageMap { get; set; } = new();

    [ObservableProperty]
    public partial StageMapResult BFVerifyStageMap { get; set; } = new();

    [ObservableProperty]
    public partial StageMapResult DFVerifyStageMap { get; set; } = new();

    public override ChuckStageMapDTO Clone() => new()
    {
        StageMapDocument = StageMapDocument,
        BFStageMap = BFStageMap.Clone(),
        DFStageMap = DFStageMap.Clone(),
        DFRepeatStageMaps = [.. DFRepeatStageMaps.Select(x => x.Clone())],
        DownloadStageMap = DownloadStageMap.Clone(),
        BFVerifyStageMap = BFVerifyStageMap.Clone(),
        DFVerifyStageMap = DFVerifyStageMap.Clone(),
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };
}
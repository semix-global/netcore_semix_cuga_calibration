using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Alignment;
using Core.Models.Models.Common.StageMap;
using Local.SQL.Cache.Providers.Bases;
using StageMapResult = Core.Models.Models.Common.StageMap.StageMap;

namespace Core.Models.Models.Chuck.StageMap;

[CacheVersion("2.0.0")]
public sealed partial class ChuckStageMapDTO : CalibrationDTOBase<ChuckStageMapDTO> /*, IAdaptTo<CalibrationChuckStageMap>*/
{
    [ObservableProperty]
    public partial AlignmentResultDto BFAlignmentResult { get; set; } = new();

    [ObservableProperty]
    public partial bool IsDarkFieldAlignment { get; set; }

    [ObservableProperty]
    public partial AlignmentResultDto DFAlignmentResult { get; set; } = new();

    [Newtonsoft.Json.JsonIgnore]
    [ObservableProperty]
    public partial StageMapDocument BFStageMapDocument { get; set; } = new();

    [Newtonsoft.Json.JsonIgnore]
    [ObservableProperty]
    public partial StageMapDocument DFStageMapDocument { get; set; } = new();

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
        BFAlignmentResult = BFAlignmentResult.Clone(),
        IsDarkFieldAlignment = IsDarkFieldAlignment,
        DFAlignmentResult = DFAlignmentResult.Clone(),
        BFStageMapDocument = BFStageMapDocument,
        DFStageMapDocument = DFStageMapDocument,
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
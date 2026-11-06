using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Extensions;
using Core.Models.Models.Common.Alignment;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Common.StageMap;
using Core.Wcf.Models.Chuck;
using Cuga.Data.DataStruct.DTO.Swath;
using Cuga.Data.DataStruct.Microscope.Enums;
using Cuga.Data.DataStruct.Optics;
using Cuga.Data.DataStruct.Stage;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Extensions;
using Net.Utilities.Models.Geometries;
using StageMapResult = Core.Models.Models.Common.StageMap.StageMap;

namespace Core.Models.Models.Chuck.StageMap;

[CacheVersion("2.0.0")]
public sealed partial class ChuckStageMapDTO : CalibrationDTOBase<ChuckStageMapDTO>, IAdaptTo<CalibrationChuckStageMap>
{
    [ObservableProperty]
    public partial MicroscopeLensInformation MicroscopeLensInformation { get; set; } = MicroscopeLensInformation.Default;

    [ObservableProperty]
    public partial ProductivityInformation DFProductivityInformation { get; set; } = ProductivityInformation.Default;

    #region BF

    [ObservableProperty]
    public partial AlignmentResultDto BFAlignmentResult { get; set; } = new();

    [Newtonsoft.Json.JsonIgnore]
    [ObservableProperty]
    public partial StageMapDocument BFStageMapDocument { get; set; } = new();

    [ObservableProperty]
    public partial StageMapResult BFStageMap { get; set; } = new();

    #endregion

    #region DF

    [ObservableProperty]
    public partial bool IsDarkFieldAlignment { get; set; }

    [ObservableProperty]
    public partial AlignmentResultDto DFAlignmentResult { get; set; } = new();

    [Newtonsoft.Json.JsonIgnore]
    [ObservableProperty]
    public partial StageMapDocument DFStageMapDocument { get; set; } = new();

    [ObservableProperty]
    public partial StageMapResult DFStageMap { get; set; } = new();

    [ObservableProperty]
    public partial StageMapResult[] DFRepeatStageMaps { get; set; } = [];

    #endregion

    [ObservableProperty]
    public partial StageMapResult ExpandStageMap { get; set; } = new();

    [ObservableProperty]
    public partial StageMapResult BFVerifyStageMap { get; set; } = new();

    [ObservableProperty]
    public partial Vector BFVerifyMaxError { get; set; }

    [ObservableProperty]
    public partial StageMapResult DFVerifyStageMap { get; set; } = new();

    [ObservableProperty]
    public partial Vector DFVerifyMaxError { get; set; }

    partial void OnDFRepeatStageMapsChanged(StageMapResult[] value)
    {
        foreach (var (index, item) in value.Index()) item.DisplayIndex = index + 1;
    }

    public override ChuckStageMapDTO Clone() => new()
    {
        MicroscopeLensInformation = MicroscopeLensInformation.Clone(),
        DFProductivityInformation = DFProductivityInformation.Clone(),
        BFAlignmentResult = BFAlignmentResult.Clone(),
        BFStageMapDocument = BFStageMapDocument,
        BFStageMap = BFStageMap.Clone(),
        IsDarkFieldAlignment = IsDarkFieldAlignment,
        DFAlignmentResult = DFAlignmentResult.Clone(),
        DFStageMapDocument = DFStageMapDocument,
        DFStageMap = DFStageMap.Clone(),
        DFRepeatStageMaps = [.. DFRepeatStageMaps.Select(t => t.Clone())],
        ExpandStageMap = ExpandStageMap.Clone(),
        BFVerifyStageMap = BFVerifyStageMap.Clone(),
        BFVerifyMaxError = BFVerifyMaxError,
        DFVerifyStageMap = DFVerifyStageMap.Clone(),
        DFVerifyMaxError = DFVerifyMaxError,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };

    public CalibrationChuckStageMap AdaptTo()
    {
        var (yLength, xLength) = ExpandStageMap.IdealMatrix.GetYXLength();

        var expandStageMap = new Wcf.Models.Chuck.StageMap
        {
            IdealBasePoint = (yLength > 0 && xLength > 0 ? ExpandStageMap.IdealMatrix[0][0] : Point.Origin).ToCgPoint(),
            RowNumber = yLength,
            ColumnNumber = xLength,
            ColumnCellWidth = yLength >= 2 && xLength >= 2 ? ExpandStageMap.IdealMatrix[0][1].X - ExpandStageMap.IdealMatrix[0][0].X : 0,
            RowCellHeight = yLength >= 2 && xLength >= 2 ? ExpandStageMap.IdealMatrix[1][0].Y - ExpandStageMap.IdealMatrix[0][0].Y : 0,
            ErrorMatrix =
            [
                .. ExpandStageMap.ErrorMatrix.Select<Vector[], CgPoint[]>(t =>
                [
                    .. t.Select(tt => tt.ToCgPoint())
                ])
            ]
        };

        return new CalibrationChuckStageMap
        {
            CgMicroscopeLens = MicroscopeLensInformation != MicroscopeLensInformation.Default ? MicroscopeLensInformation.AdaptTo().LensCode : CgMicroscopeLens.None,
            OpticsMagTypeEnum = DFProductivityInformation != ProductivityInformation.Default ? DFProductivityInformation.AdaptTo().Mag.ToCgMagTypeEnum() : CgMagTypeEnum.ErrorCgMagTypeEnum,
            Speed = DFProductivityInformation != ProductivityInformation.Default ? DFProductivityInformation.AdaptTo().Speed.ToCgSpeedLevelType() : CgSpeedLevelType.ErrorCgSpeedLevelType,
            ExpandStageMap = expandStageMap,
            IsCalibrated = IsCalibrated,
            IsVerified = IsVerified,
            IsRequiredCalibrate = IsRequiredSelfCheck
        };
    }
}
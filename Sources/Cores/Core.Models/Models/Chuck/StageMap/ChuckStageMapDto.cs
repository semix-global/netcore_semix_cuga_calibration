using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Extensions;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Common.StageMap;
using Core.Wcf.Models.Chuck;
using Cuga.Data.DataStruct.DTO.Swath;
using Cuga.Data.DataStruct.Microscope.Enums;
using Cuga.Data.DataStruct.Optics;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Chuck.StageMap;

[CacheVersion("1.0.0")]
public sealed partial class ChuckStageMapDto : CalibrationDTOBase<ChuckStageMapDto>, IAdaptTo<CalibrationChuckStageMap>
{
    [ObservableProperty]
    public partial MicroscopeLensInformation HighMicroscopeLensInformation { get; set; } = MicroscopeLensInformation.Default;

    [ObservableProperty]
    public partial ProductivityInformation ProductivityInformation { get; set; } = ProductivityInformation.Default;

    [ObservableProperty]
    public partial StageMapDto CalibrationBrightFieldStageMap { get; set; } = new();

    [ObservableProperty]
    public partial StageMapDto CalibrationDarkFieldStageMap { get; set; } = new();

    [ObservableProperty]
    public partial StageMapDto ExpandStageMapDto { get; set; } = new();

    [ObservableProperty]
    public partial StageMapDto VerifyDarkFieldStageMap { get; set; } = new();

    [ObservableProperty]
    public partial StageMapDto VerifyBrightFieldStageMap { get; set; } = new();

    [ObservableProperty]
    public partial bool IsCalibrationBrightField { get; set; }

    [ObservableProperty]
    public partial bool IsVerifyDarkField { get; set; }

    [ObservableProperty]
    public partial bool IsVerifyBrightField { get; set; }

    #region Mapper

    public override ChuckStageMapDto Clone() => new()
    {
        HighMicroscopeLensInformation = HighMicroscopeLensInformation.Clone(),
        ProductivityInformation = ProductivityInformation.Clone(),
        CalibrationBrightFieldStageMap = CalibrationBrightFieldStageMap.Clone(),
        CalibrationDarkFieldStageMap = CalibrationDarkFieldStageMap.Clone(),
        ExpandStageMapDto = ExpandStageMapDto.Clone(),
        VerifyDarkFieldStageMap = VerifyDarkFieldStageMap.Clone(),
        VerifyBrightFieldStageMap = VerifyBrightFieldStageMap.Clone(),
        IsCalibrationBrightField = IsCalibrationBrightField,
        IsVerifyDarkField = IsVerifyDarkField,
        IsVerifyBrightField = IsVerifyBrightField,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };

    public CalibrationChuckStageMap AdaptTo() => new()
    {
        CgMicroscopeLens = HighMicroscopeLensInformation != MicroscopeLensInformation.Default ? HighMicroscopeLensInformation.AdaptTo().LensCode : CgMicroscopeLens.None,
        OpticsMagTypeEnum = ProductivityInformation != ProductivityInformation.Default ? ProductivityInformation.AdaptTo().Mag.ToCgMagTypeEnum() : CgMagTypeEnum.ErrorCgMagTypeEnum,
        Speed = ProductivityInformation != ProductivityInformation.Default ? ProductivityInformation.AdaptTo().Speed.ToCgSpeedLevelType() : CgSpeedLevelType.ErrorCgSpeedLevelType,
        ExpandStageMap = ExpandStageMapDto.AdaptTo(),
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredCalibrate = IsRequiredSelfCheck
    };

    #endregion Mapper
}
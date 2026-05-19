using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Extensions;
using Core.Models.Models.Common.Pattern;
using Core.Wcf.Models.Chuck;
using Cuga.Data.DataStruct.DTO.Swath;
using Cuga.Data.DataStruct.Optics;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Chuck.AlignmentDegreeOffset;

[CacheVersion("1.0.0")]
public sealed partial class ChuckAlignmentDegreeOffsetItemDto : CalibrationDTOBase<ChuckAlignmentDegreeOffsetItemDto>, IAdaptTo<CalibrationChuckAlignmentDegreeOffsetItem>
{
    [ObservableProperty]
    public partial ProductivityInformation ProductivityInformation { get; set; } = ProductivityInformation.Default;

    [ObservableProperty]
    public partial double BrightFieldAlignmentDegree { get; set; }

    [ObservableProperty]
    public partial double DarkFieldAlignmentDegree { get; set; }

    [ObservableProperty]
    public partial double DarkFieldAlignmentVerifyResult { get; set; }

    public double DegreeOffset => DarkFieldAlignmentDegree - BrightFieldAlignmentDegree;

    #region Mapper

    public override ChuckAlignmentDegreeOffsetItemDto Clone() => new()
    {
        ProductivityInformation = ProductivityInformation.Clone(),
        BrightFieldAlignmentDegree = BrightFieldAlignmentDegree,
        DarkFieldAlignmentDegree = DarkFieldAlignmentDegree,
        DarkFieldAlignmentVerifyResult = DarkFieldAlignmentVerifyResult,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };

    public CalibrationChuckAlignmentDegreeOffsetItem AdaptTo() => new()
    {
        CgNIOITypeEnum = ProductivityInformation != ProductivityInformation.Default ? ProductivityInformation.OpticsIlluminationModeEnum.ToCgNIOITypeEnum() : CgNIOIType.ErrorCgNIOIType,
        CgMagTypeEnum = ProductivityInformation != ProductivityInformation.Default ? ProductivityInformation.AdaptTo().Mag.ToCgMagTypeEnum() : CgMagTypeEnum.ErrorCgMagTypeEnum,
        Speed = ProductivityInformation != ProductivityInformation.Default ? ProductivityInformation.AdaptTo().Speed.ToCgSpeedLevelType() : CgSpeedLevelType.ErrorCgSpeedLevelType,
        DegreeOffset = DegreeOffset,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredCalibrate = IsRequiredSelfCheck
    };

    #endregion Mapper
}
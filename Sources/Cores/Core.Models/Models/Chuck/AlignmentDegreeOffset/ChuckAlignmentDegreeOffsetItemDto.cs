using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Extensions;
using Core.Models.Models.Common.Pattern;
using Core.Wcf.Models.Chuck;
using Cuga.Data.DataStruct.DTO.Swath;
using Cuga.Data.DataStruct.Optics;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Chuck.AlignmentDegreeOffset;

[CacheVersion("1.0.0")]
public sealed partial class ChuckAlignmentDegreeOffsetItemDto : CalibrationDtoBase, ICloneable<ChuckAlignmentDegreeOffsetItemDto>, IAdaptTo<CalibrationChuckAlignmentDegreeOffsetItem>
{
    [ObservableProperty]
    private OpticsIlluminationModeEnum _opticsIlluminationMode = OpticsIlluminationModeEnum.OI;

    [ObservableProperty]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [ObservableProperty]
    private double _brightFieldAlignmentDegree;

    [ObservableProperty]
    private double _darkFieldAlignmentDegree;

    [ObservableProperty]
    private double _darkFieldAlignmentVerifyResult;

    public double DegreeOffset => DarkFieldAlignmentDegree - BrightFieldAlignmentDegree;

    #region Mapper

    public ChuckAlignmentDegreeOffsetItemDto Clone() => new()
    {
        OpticsIlluminationMode = OpticsIlluminationMode,
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
        CgNIOITypeEnum = OpticsIlluminationMode.ToCgNIOITypeEnum(),
        CgMagTypeEnum = ProductivityInformation != ProductivityInformation.Default ? ProductivityInformation.AdaptTo().Mag.ToCgMagTypeEnum() : CgMagTypeEnum.ErrorCgMagTypeEnum,
        Speed = ProductivityInformation != ProductivityInformation.Default ? ProductivityInformation.AdaptTo().Speed.ToCgSpeedLevelType() : CgSpeedLevelType.ErrorCgSpeedLevelType,
        DegreeOffset = DegreeOffset,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredCalibrate = IsRequiredSelfCheck
    };

    #endregion Mapper
}
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Stage;
using Core.Models.Extensions;
using Core.Models.Models.Common.AutoFocus;
using Core.Models.Models.Common.Pattern;
using Core.Wcf.Models.AutoFocus;
using Cuga.Data.DataStruct.DTO.Swath;
using Cuga.Data.DataStruct.Optics;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.AutoFocus.GlobalFocusOffset;

[CacheVersion("1.0.0")]
public sealed partial class AutoFocusGlobalFocusOffsetDTO : CalibrationDTOBase<AutoFocusGlobalFocusOffsetDTO>, IAdaptTo<CalibrationAutoFocusGlobalFocusOffset>
{
    [ObservableProperty]
    public partial CalChipSiteModelEnum CalChipSiteModelEnum { get; set; } = CalChipSiteModelEnum.DswModel;

    [ObservableProperty]
    public partial ProductivityInformation ProductivityInformation { get; set; } = ProductivityInformation.Default;

    [ObservableProperty]
    public partial RuntimeAfCalibrationResultDTO RuntimeAfCalibrationResultDTO { get; set; } = new();

    public override AutoFocusGlobalFocusOffsetDTO Clone() => new()
    {
        CalChipSiteModelEnum = CalChipSiteModelEnum,
        ProductivityInformation = ProductivityInformation.Clone(),
        RuntimeAfCalibrationResultDTO = RuntimeAfCalibrationResultDTO.Clone(),
        Id = Id,
        Expiration = Expiration,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck
    };

    public CalibrationAutoFocusGlobalFocusOffset AdaptTo() => new()
    {
        CgNIOITypeEnum = ProductivityInformation != ProductivityInformation.Default ? ProductivityInformation.OpticsIlluminationModeEnum.ToCgNIOITypeEnum() : CgNIOIType.ErrorCgNIOIType,
        CgMagTypeEnum = ProductivityInformation != ProductivityInformation.Default ? ProductivityInformation.AdaptTo().Mag.ToCgMagTypeEnum() : CgMagTypeEnum.ErrorCgMagTypeEnum,
        Speed = ProductivityInformation != ProductivityInformation.Default ? ProductivityInformation.AdaptTo().Speed.ToCgSpeedLevelType() : CgSpeedLevelType.ErrorCgSpeedLevelType,
        IsAFServo = RuntimeAfCalibrationResultDTO.IsAFServo,
        ECSValue = RuntimeAfCalibrationResultDTO.ECSValue,
        MotorValue = RuntimeAfCalibrationResultDTO.MotorValue,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredCalibrate = IsRequiredSelfCheck
    };
}
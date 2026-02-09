using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Stage;
using Core.Models.Extensions;
using Core.Models.Models.Common.AutoFocus;
using Core.Models.Models.Common.Pattern;
using Core.Wcf.Models.AutoFocus;
using Cuga.Data.DataStruct.DTO.Swath;
using Cuga.Data.DataStruct.Optics;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.AutoFocus.GlobalFocusOffset;

public sealed partial class GlobalFocusOffsetDTO : CalibrationDtoBase, ICloneable<GlobalFocusOffsetDTO>, IAdaptTo<CalibrationGlobalFocusOffset>
{
    [ObservableProperty]
    private CalChipSiteModelEnum _calChipSiteModelEnum = CalChipSiteModelEnum.DswModel;

    [ObservableProperty]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [ObservableProperty]
    private RuntimeAfCalibrationResultDTO _runtimeAfCalibrationResultDTO = new();

    public GlobalFocusOffsetDTO Clone() => new()
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

    public CalibrationGlobalFocusOffset AdaptTo() => new()
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
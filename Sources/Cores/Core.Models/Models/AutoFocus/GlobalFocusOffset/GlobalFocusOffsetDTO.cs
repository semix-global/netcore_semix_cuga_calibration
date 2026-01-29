using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Stage;
using Core.Models.Extensions;
using Core.Models.Models.Common.AutoFocus;
using Core.Models.Models.Common.Pattern;
using Core.Wcf.Models.AutoFocus;
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
        CgNIOITypeEnum = ProductivityInformation.OpticsIlluminationModeEnum.ToCgNIOITypeEnum(),
        CgMagTypeEnum = ProductivityInformation.AdaptTo().Mag.ToCgMagTypeEnum(),
        Speed = ProductivityInformation.AdaptTo().Speed.ToCgSpeedLevelType(),
        IsAFServo = RuntimeAfCalibrationResultDTO.IsAFServo,
        ECSValue = RuntimeAfCalibrationResultDTO.ECSValue,
        MotorValue = RuntimeAfCalibrationResultDTO.MotorValue,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredCalibrate = IsRequiredSelfCheck
    };
}
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Extensions;
using Core.Models.Models.Common.Pattern;
using Core.Wcf.Models.Fourier;
using Cuga.Data.DataStruct.DTO.Swath;
using Cuga.Data.DataStruct.Optics;
using Net.Utilities.Mapper.Interfaces;


namespace Core.Models.Models.Fourier.CenterChannelSpecularBlocker;

public sealed partial class PupilCenterChannelSpecularBlockerDTO : CalibrationDTOBase<PupilCenterChannelSpecularBlockerDTO>, IAdaptTo<CalibrationPupilCenterChannelSpecularBlocker>
{
    [ObservableProperty]
    public partial OpticsIlluminationModeEnum OpticsIlluminationMode { get; set; } = OpticsIlluminationModeEnum.OI;

    [ObservableProperty]
    public partial ProductivityInformation ProductivityInformation { get; set; } = ProductivityInformation.Default;

    [ObservableProperty]
    public partial float Ch3Angle { get; set; } = 0.3f;

    [ObservableProperty]
    public partial float Ch3TurnY { get; set; } = 0.3f;

    [ObservableProperty]
    public partial float Ch3Push { get; set; } = 0.3f;

    #region Mapper

    public override PupilCenterChannelSpecularBlockerDTO Clone() => new()
    {
        OpticsIlluminationMode = OpticsIlluminationMode,
        ProductivityInformation = ProductivityInformation.Clone(),
        Ch3Angle = Ch3Angle,
        Ch3TurnY = Ch3TurnY,
        Ch3Push = Ch3Push,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };

    public CalibrationPupilCenterChannelSpecularBlocker AdaptTo() => new()
    {
        CgNIOITypeEnum = ProductivityInformation.OpticsIlluminationModeEnum.ToCgNIOITypeEnum(),
        CgMagTypeEnum = ProductivityInformation != ProductivityInformation.Default ? ProductivityInformation.AdaptTo().Mag.ToCgMagTypeEnum() : CgMagTypeEnum.ErrorCgMagTypeEnum,
        Speed = ProductivityInformation != ProductivityInformation.Default ? ProductivityInformation.AdaptTo().Speed.ToCgSpeedLevelType() : CgSpeedLevelType.ErrorCgSpeedLevelType,

        Ch3Angle = Ch3Angle,
        Ch3TurnY = Ch3TurnY,
        Ch3Push = Ch3Push,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredCalibrate = IsRequiredSelfCheck
    };

    #endregion Mapper
}
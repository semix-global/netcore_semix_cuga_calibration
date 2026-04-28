using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Extensions;
using Core.Models.Models.Common.Pattern;
using Core.Wcf.Models.Fourier;
using Cuga.Data.DataStruct.DTO.Swath;
using Cuga.Data.DataStruct.Optics;
using Net.Utilities.Mapper.Interfaces;


namespace Core.Models.Models.Fourier.CenterChannelSpecularBlocker;

public sealed partial class PupilCenterChannelSpecularBlockerDTO : CalibrationDtoBase, ICloneable<PupilCenterChannelSpecularBlockerDTO>, IAdaptTo<CalibrationPupilCenterChannelSpecularBlocker>
{
    [ObservableProperty]
    private OpticsIlluminationModeEnum _opticsIlluminationMode = OpticsIlluminationModeEnum.OI;

    [ObservableProperty]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [ObservableProperty]
    public float _ch3Angle = 0.3f;

    [ObservableProperty]
    public float _ch3TurnY = 0.3f;

    [ObservableProperty]
    public float _ch3Push = 0.3f;

    #region Mapper

    public PupilCenterChannelSpecularBlockerDTO Clone()
    {
        return new PupilCenterChannelSpecularBlockerDTO
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
    }

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
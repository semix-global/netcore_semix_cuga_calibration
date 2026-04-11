using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Models.Common.Pattern;
using Core.Wcf.Models.Fourier;
using Net.Utilities.Mapper.Interfaces;


namespace Core.Models.Models.Fourier;

public sealed partial class PupilCenterChannelSpecularBlockerDTO : CalibrationDtoBase, ICloneable<PupilCenterChannelSpecularBlockerDTO>, IAdaptTo<CalibrationPupilCenterChannelSpecularBlocker>
{
    [ObservableProperty]
    private OpticsIlluminationModeEnum _opticsIlluminationMode = OpticsIlluminationModeEnum.OI;

    [ObservableProperty]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [ObservableProperty]
    public float _ch3TurnY = 0.3f;

    [ObservableProperty]
    public float _ch3Push = 0.3f;

    #region Mapper

    public PupilCenterChannelSpecularBlockerDTO Clone()
    {
        return new PupilCenterChannelSpecularBlockerDTO
        {
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
        Ch3TurnY = Ch3TurnY,
        Ch3Push = Ch3Push,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredCalibrate = IsRequiredSelfCheck
    };

    #endregion Mapper
}
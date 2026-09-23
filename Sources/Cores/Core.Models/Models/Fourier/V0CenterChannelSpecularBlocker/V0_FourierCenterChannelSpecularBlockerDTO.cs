using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Extensions;
using Core.Models.Models.Common.Pattern;
using Core.Wcf.Models.Fourier;
using Cuga.Data.DataStruct.DTO.Swath;
using Cuga.Data.DataStruct.Optics;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.WPF.MVVM;

namespace Core.Models.Models.Fourier.V0CenterChannelSpecularBlocker;

[CacheVersion("2.0.0")]
public sealed partial class V0FourierCenterChannelSpecularBlockerDTO : CalibrationDTOBase<V0FourierCenterChannelSpecularBlockerDTO>, IAdaptTo<CalibrationPupilCenterChannelSpecularBlocker>, IDisposable
{
    [ObservableProperty]
    public partial ProductivityInformation ProductivityInformation { get; set; } = ProductivityInformation.Default;

    public V0FourierCenterChannelSpecularBlockerDTOItem Channel3Item { get; private init; } = new(3);

    [Newtonsoft.Json.JsonConstructor]
    private V0FourierCenterChannelSpecularBlockerDTO(V0FourierCenterChannelSpecularBlockerDTOItem channel3Item)
    {
        Channel3Item = channel3Item;
    }

    public V0FourierCenterChannelSpecularBlockerDTO()
    {
    }

    #region Mapper

#pragma warning disable IDISP003

    public override V0FourierCenterChannelSpecularBlockerDTO Clone() => new()
    {
        ProductivityInformation = ProductivityInformation.Clone(),
        Channel3Item = Channel3Item.Clone(),
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };

#pragma warning restore IDISP003

    public CalibrationPupilCenterChannelSpecularBlocker AdaptTo() => new()
    {
        CgNIOITypeEnum = ProductivityInformation != ProductivityInformation.Default ? ProductivityInformation.OpticsIlluminationModeEnum.ToCgNIOITypeEnum() : CgNIOIType.ErrorCgNIOIType,
        CgMagTypeEnum = ProductivityInformation != ProductivityInformation.Default ? ProductivityInformation.AdaptTo().Mag.ToCgMagTypeEnum() : CgMagTypeEnum.ErrorCgMagTypeEnum,
        Speed = ProductivityInformation != ProductivityInformation.Default ? ProductivityInformation.AdaptTo().Speed.ToCgSpeedLevelType() : CgSpeedLevelType.ErrorCgSpeedLevelType,
        Ch3Angle = (float)Channel3Item.YRodAngleMotorAbsoluteValue,
        Ch3TurnY = (float)Channel3Item.YRodMotorAbsoluteValue,
        Ch3Push = (float)Channel3Item.PushMotorAbsoluteValue,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredCalibrate = IsRequiredSelfCheck
    };

    #endregion Mapper

    public void Dispose()
    {
        Channel3Item.Dispose();
    }
}

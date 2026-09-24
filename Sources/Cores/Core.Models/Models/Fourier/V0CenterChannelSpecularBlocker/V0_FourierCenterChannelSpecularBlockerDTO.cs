using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Extensions;
using Core.Models.Models.Common.Pattern;
using Core.Wcf.Models.Fourier;
using Cuga.Data.DataStruct.DTO.Swath;
using Cuga.Data.DataStruct.Optics;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Fourier.V0CenterChannelSpecularBlocker;

[CacheVersion("2.0.0")]
public sealed partial class V0FourierCenterChannelSpecularBlockerDTO : CalibrationDTOBase<V0FourierCenterChannelSpecularBlockerDTO>, IAdaptTo<CalibrationPupilCenterChannelSpecularBlocker>, IDisposable
{
    [ObservableProperty]
    public partial ProductivityInformation ProductivityInformation { get; set; } = ProductivityInformation.Default;

    [Newtonsoft.Json.JsonProperty(ObjectCreationHandling = Newtonsoft.Json.ObjectCreationHandling.Replace)]
    public V0FourierCenterChannelSpecularBlockerDTOItem Item { get; private init; } = new();

    #region Mapper

#pragma warning disable IDISP003

    public override V0FourierCenterChannelSpecularBlockerDTO Clone() => new()
    {
        ProductivityInformation = ProductivityInformation.Clone(),
        Item = Item.Clone(),
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
        Ch3Angle = (float)Item.YRodAngleMotorAbsoluteValue,
        Ch3TurnY = (float)Item.YRodROI.MotorAbsoluteValue,
        Ch3Push = (float)Item.XPushROI.MotorAbsoluteValue,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredCalibrate = IsRequiredSelfCheck
    };

    #endregion Mapper

    public void Dispose()
    {
        Item.Dispose();
    }
}
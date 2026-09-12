using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Extensions;
using Core.Models.Models.Common.Pattern;
using Core.Wcf.Models.Fourier;
using Cuga.Data.DataStruct.DTO.Swath;
using Cuga.Data.DataStruct.Optics;
using Local.SQL.Cache.Providers.Bases;
using Microsoft.Extensions.Logging;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.WPF.MVVM;

namespace Core.Models.Models.Fourier.SideChannelSpecularBlocker;

[CacheVersion("1.0.0")]
public sealed partial class FourierSideChannelSpecularBlockerDTO(int rodTotalCount) : CalibrationDTOBase<FourierSideChannelSpecularBlockerDTO>, IAdaptTo<CalibrationPupilSideChannelSpecularBlocker>, IDisposable
{
    internal const int DefaultRodTotalCount = 46;

    [Newtonsoft.Json.JsonProperty]
    private readonly int _rodTotalCount = rodTotalCount;

    [ObservableProperty]
    public partial ProductivityInformation ProductivityInformation { get; set; } = ProductivityInformation.Default;

    public FourierSideChannelSpecularBlockerDTOItem Channel1Item { get; private init; } = new(rodTotalCount, 1);

    public FourierSideChannelSpecularBlockerDTOItem Channel2Item { get; private init; } = new(rodTotalCount, 2);

    public FourierSideChannelSpecularBlockerDTO() : this(DefaultRodTotalCount)
    {
    }

    #region Mapper

#pragma warning disable IDISP003

    public override FourierSideChannelSpecularBlockerDTO Clone() => new(_rodTotalCount)
    {
        ProductivityInformation = ProductivityInformation.Clone(),
        Channel1Item = Channel1Item.Clone(),
        Channel2Item = Channel2Item.Clone(),
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };

#pragma warning restore IDISP003

    public CalibrationPupilSideChannelSpecularBlocker AdaptTo()
    {
        if (IsCalibrated == false
            || Channel1Item.Rods.Length != _rodTotalCount
            || Channel2Item.Rods.Length != _rodTotalCount
            || Channel1Item.MoveDownPercents.Length != _rodTotalCount
            || Channel2Item.MoveDownPercents.Length != _rodTotalCount)
        {
            if (IsCalibrated)
            {
                var logger = HostApplication.GetRequiredService<ILogger<FourierSideChannelSpecularBlockerDTO>>();

                if (Channel1Item.Rods.Length != _rodTotalCount) logger.LogError("Channel 1 rod count is not equal to {@RodTotalCount}.", _rodTotalCount);
                if (Channel2Item.Rods.Length != _rodTotalCount) logger.LogError("Channel 2 rod count is not equal to {@RodTotalCount}.", _rodTotalCount);
            }

            return new CalibrationPupilSideChannelSpecularBlocker
            {
                IsCalibrated = false,
                IsVerified = false,
                IsRequiredCalibrate = IsRequiredSelfCheck
            };
        }

        return new CalibrationPupilSideChannelSpecularBlocker
        {
            CgNIOITypeEnum = ProductivityInformation != ProductivityInformation.Default ? ProductivityInformation.OpticsIlluminationModeEnum.ToCgNIOITypeEnum() : CgNIOIType.ErrorCgNIOIType,
            CgMagTypeEnum = ProductivityInformation != ProductivityInformation.Default ? ProductivityInformation.AdaptTo().Mag.ToCgMagTypeEnum() : CgMagTypeEnum.ErrorCgMagTypeEnum,
            Speed = ProductivityInformation != ProductivityInformation.Default ? ProductivityInformation.AdaptTo().Speed.ToCgSpeedLevelType() : CgSpeedLevelType.ErrorCgSpeedLevelType,
            CgFFBoxMoveDownPercentListCh1 = [.. Channel1Item.Rods.OrderBy(t => t.Index).Select(t => Channel1Item.MoveDownPercents[t.Index])],
            CgFFBoxMoveDownPercentListCh2 = [.. Channel2Item.Rods.OrderBy(t => t.Index).Select(t => Channel2Item.MoveDownPercents[t.Index])],
            IsCalibrated = IsCalibrated,
            IsVerified = IsVerified,
            IsRequiredCalibrate = IsRequiredSelfCheck
        };
    }

    #endregion Mapper

    public void Dispose()
    {
        Channel1Item.Dispose();
        Channel2Item.Dispose();
    }
}

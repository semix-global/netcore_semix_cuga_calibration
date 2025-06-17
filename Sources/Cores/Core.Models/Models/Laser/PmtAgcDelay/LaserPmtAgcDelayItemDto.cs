using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Microscope;
using Core.Models.Enums.Optics;
using Core.Models.Extensions;
using Core.Wcf.Models.Laser;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models;
using EnumOpticsExtension = Core.Models.Extensions.EnumOpticsExtension;

namespace Core.Models.Models.Laser.PmtAgcDelay;

public sealed partial class LaserPmtAgcDelayItemDto : CalibrationDtoBase, ICloneable<LaserPmtAgcDelayItemDto>, IAdaptTo<CalibrationLaserPmtAgcDelayItem>
{
    [ObservableProperty]
    private MicroscopeMagnificationEnum _microscopeMagnificationEnum;

    [ObservableProperty]
    private OpticsMagTypeEnum _opticsMagTypeEnum;

    [ObservableProperty]
    private int _pmtId;

    [ObservableProperty]
    private double _channel1AgcDelay;

    [ObservableProperty]
    private double _channel2AgcDelay;

    [ObservableProperty]
    private double _channel3AgcDelay;

    [ObservableProperty]
    private Point[] _channel1SenseDataPoints = [];

    [ObservableProperty]
    private Point[] _channel2SenseDataPoints = [];

    [ObservableProperty]
    private Point[] _channel3SenseDataPoints = [];

    #region Mapper

    public LaserPmtAgcDelayItemDto Clone() => new()
    {
        MicroscopeMagnificationEnum = MicroscopeMagnificationEnum,
        OpticsMagTypeEnum = OpticsMagTypeEnum,
        PmtId = PmtId,
        Channel1AgcDelay = Channel1AgcDelay,
        Channel2AgcDelay = Channel2AgcDelay,
        Channel3AgcDelay = Channel3AgcDelay,
        Channel1SenseDataPoints = [.. Channel1SenseDataPoints],
        Channel2SenseDataPoints = [.. Channel2SenseDataPoints],
        Channel3SenseDataPoints = [.. Channel3SenseDataPoints],
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };

    public CalibrationLaserPmtAgcDelayItem AdaptTo() => new()
    {
        CgMagTypeEnum = OpticsMagTypeEnum.ToCgMagTypeEnum(),
        PmtId = PmtId,
        Channel1AgcDelay = Channel1AgcDelay,
        Channel2AgcDelay = Channel2AgcDelay,
        Channel3AgcDelay = Channel3AgcDelay
    };

    #endregion Mapper
}
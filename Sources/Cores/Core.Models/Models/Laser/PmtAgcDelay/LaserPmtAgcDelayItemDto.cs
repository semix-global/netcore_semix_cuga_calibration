using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Extensions;
using Core.Wcf.Models.Laser;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Laser.PmtAgcDelay;

public sealed partial class LaserPmtAgcDelayItemDto : CalibrationDtoBase, ICloneable<LaserPmtAgcDelayItemDto>, IAdaptTo<CalibrationLaserPmtAgcDelayItem>
{
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
    private double _channel1AgcOffset;

    [ObservableProperty]
    private double _channel2AgcOffset;

    [ObservableProperty]
    private double _channel3AgcOffset;

    [ObservableProperty]
    private List<List<double>> _channel1SenseData = [];

    [ObservableProperty]
    private List<List<double>> _channel2SenseData = [];

    [ObservableProperty]
    private List<List<double>> _channel3SenseData = [];

    #region Mapper

    public LaserPmtAgcDelayItemDto Clone() => new()
    {
        OpticsMagTypeEnum = OpticsMagTypeEnum,
        PmtId = PmtId,
        Channel1AgcDelay = Channel1AgcDelay,
        Channel2AgcDelay = Channel2AgcDelay,
        Channel3AgcDelay = Channel3AgcDelay,
        Channel1AgcOffset = Channel1AgcOffset,
        Channel2AgcOffset = Channel2AgcOffset,
        Channel3AgcOffset = Channel3AgcOffset,
        Channel1SenseData =
        [
            .. Channel1SenseData.Select<List<double>, List<double>>(t =>
            [
                .. t
            ])
        ],
        Channel2SenseData =
        [
            .. Channel2SenseData.Select<List<double>, List<double>>(t =>
            [
                .. t
            ])
        ],
        Channel3SenseData =
        [
            .. Channel3SenseData.Select<List<double>, List<double>>(t =>
            [
                .. t
            ])
        ],
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
using CommunityToolkit.Mvvm.ComponentModel;
using Local.NoSQL.DB.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models;

namespace Core.Models.Models.Laser.PmtGain;

public sealed partial class LaserPmtGainDto : CalibrationDtoBase, ICloneable<LaserPmtGainDto>
{
    [ObservableProperty]
    private int _pmtId;

    [ObservableProperty]
    private int _channel;

    [ObservableProperty]
    private string _measurePower = string.Empty;

    [ObservableProperty]
    private double _prescanAod;

    [ObservableProperty]
    private Point _measurePosition;

    [ObservableProperty]
    private List<LaserPmtGainItemDto> _plot = [];

    #region Mapper

    public LaserPmtGainDto Clone()
    {
        return new LaserPmtGainDto
        {
            PmtId = PmtId,
            Channel = Channel,
            MeasurePower = MeasurePower,
            PrescanAod = PrescanAod,
            MeasurePosition = MeasurePosition,
            Plot = [.. Plot.Select(t => t.Clone())],
            IsCalibrated = IsCalibrated,
            IsVerified = IsVerified,
            IsRequiredSelfCheck = IsRequiredSelfCheck,
            Id = Id,
            Expiration = Expiration
        };
    }

    #endregion Mapper
}

public sealed partial class LaserPmtGainItemDto : ObservableCacheBase, ICloneable<LaserPmtGainItemDto>
{
    [ObservableProperty]
    private string _measurePower = string.Empty;

    [ObservableProperty]
    private double _agingPower;

    [ObservableProperty]
    private double _currentPower;

    [ObservableProperty]
    private double _currentVoltage;

    [ObservableProperty]
    private bool _isAging;

    [ObservableProperty]
    private List<Point> _voltageLightListPoint = [];

    #region Mapper

    public LaserPmtGainItemDto Clone() => new()
    {
        MeasurePower = MeasurePower,
        AgingPower = AgingPower,
        CurrentPower = CurrentPower,
        CurrentVoltage = CurrentVoltage,
        IsAging = IsAging,
        VoltageLightListPoint = [.. VoltageLightListPoint],
        Id = Id,
        Expiration = Expiration
    };

    #endregion Mapper
}
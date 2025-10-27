using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Extensions;
using Core.Wcf.Models.Laser;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.AOD.AODDelay;

public sealed partial class AODDelayDto : CalibrationDtoBase, ICloneable<AODDelayDto>, IAdaptTo<CalibrationLaserAodDelayItem>
{
    [ObservableProperty]
    private double _index;

    [ObservableProperty]
    private OpticsMagTypeEnum _opticsMagTypeEnum;

    [ObservableProperty]
    private double _roughAodDelayTime;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RefinedPrescanAodDelayTime), nameof(RefinedChirpAodDelayTime))]
    private double _refinedAodDelayTime;

    public double RefinedPrescanAodDelayTime => RefinedAodDelayTime >= 0 ? 0 : Math.Abs((double)RefinedAodDelayTime);

    public double RefinedChirpAodDelayTime => RefinedAodDelayTime <= 0 ? 0 : Math.Abs((double)RefinedAodDelayTime);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AveragePmtData))]
    private List<double> _pmtDataList = [];

    public double AveragePmtData => PmtDataList.Count > 0 ? Enumerable.Average((IEnumerable<double>)PmtDataList) : 0d;

    #region Mapper

    public AODDelayDto Clone() => new()
    {
        Index = Index,
        OpticsMagTypeEnum = OpticsMagTypeEnum,
        RoughAodDelayTime = RoughAodDelayTime,
        RefinedAodDelayTime = RefinedAodDelayTime,
        PmtDataList = [.. PmtDataList],
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };

    public CalibrationLaserAodDelayItem AdaptTo() => new()
    {
        CgMagTypeEnum = OpticsMagTypeEnum.ToCgMagTypeEnum(),
        PrescanAodDelayTime = RefinedPrescanAodDelayTime,
        ChirpAodDelayTime = RefinedChirpAodDelayTime,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck
    };

    #endregion Mapper
}
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Extensions;
using Core.Models.Models.Common.Pattern;
using Core.Wcf.Models.Laser;
using LiteDB;
using Net.Utilities.Mapper.Interfaces;
using Newtonsoft.Json;

namespace Core.Models.Models.AOD.AODDelay;

public sealed partial class AODDelayDto : CalibrationDtoBase, ICloneable<AODDelayDto>, IAdaptTo<CalibrationLaserAodDelayItem>
{
    [ObservableProperty]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [ObservableProperty]
    private double _roughAODDelay;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RefinedPrescanAODDelay), nameof(RefinedChirpAODDelay))]
    private double _refinedAODDelay;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AveragePmtData))]
    private IReadOnlyList<double> _pmtData = [];

    [JsonIgnore]
    [BsonIgnore]
    public double RefinedPrescanAODDelay => RefinedAODDelay >= 0 ? 0 : Math.Abs(RefinedAODDelay);

    [JsonIgnore]
    [BsonIgnore]
    public double RefinedChirpAODDelay => RefinedAODDelay <= 0 ? 0 : Math.Abs(RefinedAODDelay);

    [JsonIgnore]
    [BsonIgnore]
    public double AveragePmtData => PmtData.Count > 0 ? PmtData.Average() : 0d;

    #region Mapper

    public AODDelayDto Clone() => new()
    {
        ProductivityInformation = ProductivityInformation.Clone(),
        RoughAODDelay = RoughAODDelay,
        RefinedAODDelay = RefinedAODDelay,
        PmtData = [.. PmtData],
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };

    public CalibrationLaserAodDelayItem AdaptTo() => new()
    {
        CgMagTypeEnum = ProductivityInformation.AdaptTo().Mag.ToCgMagTypeEnum(),
        PrescanAodDelayTime = RefinedPrescanAODDelay,
        ChirpAodDelayTime = RefinedChirpAODDelay,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck
    };

    #endregion Mapper
}
using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Laser.BeamStabilizer;

public sealed partial class LaserBeamStabilizerCache : CalibrationCacheBase<LaserBeamStabilizerCache>
{
    [ObservableProperty]
    public partial Point CurrentPDPosition1 { get; set; }

    [ObservableProperty]
    public partial Point CurrentPDPosition2 { get; set; }

    [ObservableProperty]
    public partial Point OriginPosition1 { get; set; }

    [ObservableProperty]
    public partial Point OriginPosition2 { get; set; }

    [ObservableProperty]
    public partial int Interval { get; set; }

    [ObservableProperty]
    public partial int Threshold { get; set; }

    [ObservableProperty]
    public partial int RepeatNumber { get; set; } = 5;

    public override LaserBeamStabilizerCache Clone() => new()
    {
        CurrentPDPosition1 = CurrentPDPosition1,
        CurrentPDPosition2 = CurrentPDPosition2,
        OriginPosition1 = OriginPosition1,
        OriginPosition2 = OriginPosition2,
        Interval = Interval,
        Threshold = Threshold,
        RepeatNumber = RepeatNumber,
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration
    };
}
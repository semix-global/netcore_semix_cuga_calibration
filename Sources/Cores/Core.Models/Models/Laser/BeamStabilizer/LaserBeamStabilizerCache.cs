using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Laser.BeamStabilizer;

public sealed partial class LaserBeamStabilizerCache : CalibrationCacheBase<LaserBeamStabilizerCache>
{
    [ObservableProperty]
    private Point _currentPDPosition1;

    [ObservableProperty]
    private Point _currentPDPosition2;

    [ObservableProperty]
    private Point _originPosition1;

    [ObservableProperty]
    private Point _originPosition2;

    [ObservableProperty]
    private int _interval;

    [ObservableProperty]
    private int _threshold;

    [ObservableProperty]
    private int _repeatNumber = 5;

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
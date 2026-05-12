using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Ads.PressureGains;

public sealed partial class AdsPressureGainsCache : CalibrationCacheBase<AdsPressureGainsCache>
{
    [ObservableProperty]
    private Point _findPosition;

    [ObservableProperty]
    private double _threshold;

    public override AdsPressureGainsCache Clone() => new()
    {
        FindPosition = FindPosition,
        Threshold = Threshold,
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration,
    };
}
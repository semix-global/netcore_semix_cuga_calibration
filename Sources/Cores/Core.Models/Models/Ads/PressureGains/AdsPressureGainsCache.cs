using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Ads.PressureGains;

public sealed partial class AdsPressureGainsCache : CalibrationCacheBase<AdsPressureGainsCache>
{
    [ObservableProperty]
    public partial Point FindPosition { get; set; }

    [ObservableProperty]
    public partial double Threshold { get; set; }

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
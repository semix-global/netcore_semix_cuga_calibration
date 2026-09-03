using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Ads.YGains;

public sealed partial class AdsYGainsCache : CalibrationCacheBase<AdsYGainsCache>
{
    [ObservableProperty]
    public partial Point StartPosition { get; set; }

    [ObservableProperty]
    public partial Point EndPosition { get; set; }

    [ObservableProperty]
    public partial double[] YSpeeds { get; set; } = [50, 100, 150, 200];

    [ObservableProperty]
    public partial int FindMaxY { get; set; } = 100;

    [ObservableProperty]
    public partial int FindMinY { get; set; }

    [ObservableProperty]
    public partial double Threshold { get; set; } = 50;

    [ObservableProperty]
    public partial double VerifyThreshold { get; set; } = 75;

    [ObservableProperty]
    public partial int WaitTime { get; set; } = 2;

    public override AdsYGainsCache Clone() => new()
    {
        StartPosition = StartPosition,
        EndPosition = EndPosition,
        YSpeeds = [.. YSpeeds],
        FindMaxY = FindMaxY,
        FindMinY = FindMinY,
        Threshold = Threshold,
        VerifyThreshold = VerifyThreshold,
        WaitTime = WaitTime,
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration
    };
}
using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Ads.XGains;

public sealed partial class AdsXGainsCache : CalibrationCacheBase<AdsXGainsCache>
{
    [ObservableProperty]
    public partial Point StartPosition { get; set; }

    [ObservableProperty]
    public partial Point EndPosition { get; set; }

    [ObservableProperty]
    public partial double[] XSpeeds { get; set; } = [50, 65, 85, 110, 143, 186, 242, 315, 400];

    [ObservableProperty]
    public partial int FindMaxX { get; set; } = 100;

    [ObservableProperty]
    public partial int FindMinX { get; set; }

    [ObservableProperty]
    public partial double Threshold { get; set; } = 50;

    [ObservableProperty]
    public partial double VerifyThreshold { get; set; } = 75;

    [ObservableProperty]
    public partial int WaitTime { get; set; } = 2;

    public override AdsXGainsCache Clone() => new()
    {
        XSpeeds = [.. XSpeeds],
        StartPosition = StartPosition,
        EndPosition = EndPosition,
        FindMaxX = FindMaxX,
        FindMinX = FindMinX,
        Threshold = Threshold,
        VerifyThreshold = VerifyThreshold,
        WaitTime = WaitTime,
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration
    };
}
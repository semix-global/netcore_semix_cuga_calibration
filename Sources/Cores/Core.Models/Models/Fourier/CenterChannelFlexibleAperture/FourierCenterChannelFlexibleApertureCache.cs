using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Pattern;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Fourier.CenterChannelFlexibleAperture;

[CacheVersion("2.0.0")]
public sealed partial class FourierCenterChannelFlexibleApertureCache : CalibrationCacheBase<FourierCenterChannelFlexibleApertureCache>
{
    [ObservableProperty]
    public partial ProductivityInformation ProductivityInformation { get; set; } = ProductivityInformation.Default;

    [ObservableProperty]
    public partial MicroscopeLensInformation MicroscopeLensInformation { get; set; } = MicroscopeLensInformation.Default;

    [ObservableProperty]
    public partial LaserLightInformation LaserLightInformation { get; set; } = LaserLightInformation.Default;

    [ObservableProperty]
    public partial OpticsConfiguration OpticsConfiguration { get; set; } = new();

    [ObservableProperty]
    public partial double ScanLength { get; set; } = 20d;

    [ObservableProperty]
    public partial Point HazeFindBFMachinePosition { get; set; }

    [ObservableProperty]
    public partial double Ch3XLPOSMin { get; set; }

    [ObservableProperty]
    public partial double Ch3XLPOSMax { get; set; }

    [ObservableProperty]
    public partial double Ch3XPPOSMin { get; set; }

    [ObservableProperty]
    public partial double Ch3XPPOSMax { get; set; }

    [ObservableProperty]
    public partial double Ch3YLPOSMin { get; set; }

    [ObservableProperty]
    public partial double Ch3YLPOSMax { get; set; }

    [ObservableProperty]
    public partial double PositionErrorThreshold { get; set; } = 1d;

    [ObservableProperty]
    public partial double CircleCenterErrorThreshold { get; set; } = 1d;

    [ObservableProperty]
    public partial double CircleRadiusErrorThreshold { get; set; } = 1d;

    public override FourierCenterChannelFlexibleApertureCache Clone() => new()
    {
        ProductivityInformation = ProductivityInformation.Clone(),
        MicroscopeLensInformation = MicroscopeLensInformation.Clone(),
        LaserLightInformation = LaserLightInformation.Clone(),
        OpticsConfiguration = OpticsConfiguration.Clone(),
        ScanLength = ScanLength,
        HazeFindBFMachinePosition = HazeFindBFMachinePosition,
        Ch3XLPOSMin = Ch3XLPOSMin,
        Ch3XLPOSMax = Ch3XLPOSMax,
        Ch3XPPOSMin = Ch3XPPOSMin,
        Ch3XPPOSMax = Ch3XPPOSMax,
        Ch3YLPOSMin = Ch3YLPOSMin,
        Ch3YLPOSMax = Ch3YLPOSMax,
        PositionErrorThreshold = PositionErrorThreshold,
        CircleCenterErrorThreshold = CircleCenterErrorThreshold,
        CircleRadiusErrorThreshold = CircleRadiusErrorThreshold,
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration
    };
}
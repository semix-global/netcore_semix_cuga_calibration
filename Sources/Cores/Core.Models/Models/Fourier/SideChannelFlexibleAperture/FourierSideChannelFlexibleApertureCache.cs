using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Pattern;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Fourier.SideChannelFlexibleAperture;

[CacheVersion("2.0.0")]
public sealed partial class FourierSideChannelFlexibleApertureCache : CalibrationCacheBase<FourierSideChannelFlexibleApertureCache>
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
    public partial int ScanLength { get; set; } = 1000;

    [ObservableProperty]
    public partial Point HazeFindBFMachinePosition { get; set; }

    public override FourierSideChannelFlexibleApertureCache Clone() => new()
    {
        ProductivityInformation = ProductivityInformation.Clone(),
        MicroscopeLensInformation = MicroscopeLensInformation.Clone(),
        LaserLightInformation = LaserLightInformation.Clone(),
        OpticsConfiguration = OpticsConfiguration.Clone(),
        ScanLength = ScanLength,
        HazeFindBFMachinePosition = HazeFindBFMachinePosition,
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration
    };
}
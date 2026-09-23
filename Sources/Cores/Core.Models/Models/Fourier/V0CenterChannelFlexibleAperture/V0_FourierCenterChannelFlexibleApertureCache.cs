using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Pattern;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Fourier.V0CenterChannelFlexibleAperture;

[CacheVersion("2.0.0")]
public sealed partial class V0FourierCenterChannelFlexibleApertureCache : CalibrationCacheBase<V0FourierCenterChannelFlexibleApertureCache>
{
    [ObservableProperty]
    public partial double XMinMotorAbsoluteValue { get; set; }

    [ObservableProperty]
    public partial double XMaxMotorAbsoluteValue { get; set; }

    [ObservableProperty]
    public partial double PushMinMotorAbsoluteValue { get; set; }

    [ObservableProperty]
    public partial double PushMaxMotorAbsoluteValue { get; set; }

    [ObservableProperty]
    public partial double YMinMotorAbsoluteValue { get; set; }

    [ObservableProperty]
    public partial double YMaxMotorAbsoluteValue { get; set; }

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

    public override V0FourierCenterChannelFlexibleApertureCache Clone() => new()
    {
        XMinMotorAbsoluteValue = XMinMotorAbsoluteValue,
        XMaxMotorAbsoluteValue = XMaxMotorAbsoluteValue,
        PushMinMotorAbsoluteValue = PushMinMotorAbsoluteValue,
        PushMaxMotorAbsoluteValue = PushMaxMotorAbsoluteValue,
        YMinMotorAbsoluteValue = YMinMotorAbsoluteValue,
        YMaxMotorAbsoluteValue = YMaxMotorAbsoluteValue,
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
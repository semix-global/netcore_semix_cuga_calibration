using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Models.Geometries;


namespace Core.Models.Models.Optics.CollectPolarization;

public sealed partial class CollectPolarizationCache : CalibrationCacheBase<CollectPolarizationCache>
{
    [ObservableProperty]
    public partial OpticsPolarizationModeEnum OpticsPolarizationModeEnum { get; set; } = OpticsPolarizationModeEnum.P;

    [ObservableProperty]
    public partial OpticsCollectorPolarizationModeEnum OpticsCollectorPolarizationMode { get; set; } = OpticsCollectorPolarizationModeEnum.N;

    [ObservableProperty]
    public partial Point FindBFMachinePosition { get; set; }

    [ObservableProperty]
    public partial double StartNDFRotaryMotorPos { get; set; } = 0;

    [ObservableProperty]
    public partial double StopNDFRotaryMotorPos { get; set; } = 360;

    [ObservableProperty]
    public partial double StepNDFRotaryMotorPos { get; set; } = 30;

    [ObservableProperty]
    public partial double RangeRefinedNDFRotaryMotorPos { get; set; } = 15;

    [ObservableProperty]
    public partial double StepRefinedNDFRotaryMotorPos { get; set; } = 1;

    [ObservableProperty]
    public partial int ImageWidth { get; set; } = 1000;

    [ObservableProperty]
    public partial double Threshold { get; set; } = 20d;

    [ObservableProperty]
    public partial ProductivityInformation ProductivityInformation { get; set; } = ProductivityInformation.Default;

    [ObservableProperty]
    public partial LaserLightInformation LaserLightInformation { get; set; } = LaserLightInformation.Default;

    [ObservableProperty]
    public partial int PMTId { get; set; } = -1;

    [ObservableProperty]
    public partial CIBConfiguration CIBConfiguration { get; set; } = new();

    public override CollectPolarizationCache Clone() => new()
    {
        OpticsPolarizationModeEnum = OpticsPolarizationModeEnum,
        OpticsCollectorPolarizationMode = OpticsCollectorPolarizationMode,
        FindBFMachinePosition = FindBFMachinePosition,
        StartNDFRotaryMotorPos = StartNDFRotaryMotorPos,
        StopNDFRotaryMotorPos = StopNDFRotaryMotorPos,
        StepNDFRotaryMotorPos = StepNDFRotaryMotorPos,
        RangeRefinedNDFRotaryMotorPos = RangeRefinedNDFRotaryMotorPos,
        StepRefinedNDFRotaryMotorPos = StepRefinedNDFRotaryMotorPos,
        ImageWidth = ImageWidth,
        Threshold = Threshold,
        ProductivityInformation = ProductivityInformation.Clone(),
        LaserLightInformation = LaserLightInformation.Clone(),
        PMTId = PMTId,
        CIBConfiguration = CIBConfiguration.Clone(),
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration
    };
}
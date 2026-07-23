using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Stage;
using Core.Models.Models.Common.Alignment;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.AutoFocus.FAFBCompensation;

public sealed partial class AutoFocusFAFBCompensationCache : CalibrationCacheBase<AutoFocusFAFBCompensationCache>
{
    [ObservableProperty]
    public partial MicroscopeLensInformation MicroscopeLensInformation { get; set; } = MicroscopeLensInformation.Default;

    [ObservableProperty]
    public partial CalChipSiteModelEnum CalChipSiteModelEnum { get; set; } = CalChipSiteModelEnum.DswModel;

    [ObservableProperty]
    public partial AlignmentResultDto AlignmentResult { get; set; } = new();

    [ObservableProperty]
    public partial double SpeedECSPerSecond { get; set; } = 1000d;

    [ObservableProperty]
    public partial double ThresholdECS { get; set; } = 5d;

    [ObservableProperty]
    public partial IReadOnlyList<AutoFocusFAFBCompensationCacheItem> Items { get; set; } = [];

    public override AutoFocusFAFBCompensationCache Clone() => new()
    {
        MicroscopeLensInformation = MicroscopeLensInformation.Clone(),
        CalChipSiteModelEnum = CalChipSiteModelEnum,
        AlignmentResult = AlignmentResult.Clone(),
        SpeedECSPerSecond = SpeedECSPerSecond,
        ThresholdECS = ThresholdECS,
        Items = [.. Items.Select(t => t.Clone())],
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration
    };
}

public sealed partial class AutoFocusFAFBCompensationCacheItem : ObservableObject, ICloneable<AutoFocusFAFBCompensationCacheItem>
{
    [ObservableProperty]
    public partial Point FindBFMachinePosition { get; set; }

    public AutoFocusFAFBCompensationCacheItem Clone() => new()
    {
        FindBFMachinePosition = FindBFMachinePosition
    };
}
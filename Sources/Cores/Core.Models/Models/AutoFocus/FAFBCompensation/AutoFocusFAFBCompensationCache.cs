using CommunityToolkit.Mvvm.ComponentModel;
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
    public partial AlignmentResultDto AlignmentResult { get; set; } = new();

    [ObservableProperty]
    public partial double RangeECS { get; set; }

    [ObservableProperty]
    public partial double SpeedECSPerSecond { get; set; } = 500d;

    [ObservableProperty]
    public partial double ThresholdECS { get; set; } = 5d;

    [ObservableProperty]
    public partial IReadOnlyList<AutoFocusFAFBCompensationCacheItem> Items { get; set; } = [];

    public override AutoFocusFAFBCompensationCache Clone() => new()
    {
        MicroscopeLensInformation = MicroscopeLensInformation.Clone(),
        AlignmentResult = AlignmentResult.Clone(),
        RangeECS = RangeECS,
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
    public partial Point DSWFindBrightMachinePosition { get; set; }

    public AutoFocusFAFBCompensationCacheItem Clone() => new()
    {
        DSWFindBrightMachinePosition = DSWFindBrightMachinePosition
    };
}
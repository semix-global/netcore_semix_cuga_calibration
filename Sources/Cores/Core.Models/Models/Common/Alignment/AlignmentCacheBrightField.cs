using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Stage;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Common.Alignment;

public sealed partial class AlignmentCacheBrightField : AlignmentCacheBase, ICloneable<AlignmentCacheBrightField>
{
    [ObservableProperty]
    public partial CalChipSiteModelEnum CalChipSiteModelEnum { get; set; }

    public AlignmentCacheBrightField Clone() => new()
    {
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmWaferTypeEnum = AlgorithmWaferTypeEnum,
        LowMag = LowMag,
        LowSizeEnum = LowSizeEnum,
        HighMag = HighMag,
        HighSizeEnum = HighSizeEnum,
        LowSite1 = LowSite1,
        LowSite2 = LowSite2,
        HighSite1 = HighSite1,
        HighSite2 = HighSite2,
        Result = Result,
        IsVerified = IsVerified,
        IsOk = IsOk,
        CalChipSiteModelEnum = CalChipSiteModelEnum
    };
}
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Common.Alignment;

public sealed partial class AlignmentCacheDarkField : AlignmentCacheBase, ICloneable<AlignmentCacheDarkField>
{
    /// <summary>
    /// HighSite产率
    /// </summary>
    [ObservableProperty]
    public partial ProductivityInformation ProductivityInformation { get; set; } = ProductivityInformation.Default;

    public AlignmentCacheDarkField Clone() => new()
    {
        ProductivityInformation = ProductivityInformation.Clone(),
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        LowMag = LowMag.Clone(),
        LowSizeEnum = LowSizeEnum,
        HighMag = HighMag.Clone(),
        HighSizeEnum = HighSizeEnum,
        LowSite1 = LowSite1.Clone(),
        LowSite2 = LowSite2.Clone(),
        HighSite1 = HighSite1.Clone(),
        HighSite2 = HighSite2.Clone(),
        Result = Result.Clone(),
        IsVerified = IsVerified,
        IsOk = IsOk
    };
}
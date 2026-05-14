using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Algorithm;
using Core.Models.Models.Common.Alignment;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.DataAnnotations;
using Net.Utilities.Models.Enums.Maths;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Chuck.Prealigner;

public sealed partial class ChuckPrealignerCache : CalibrationCacheBase<ChuckPrealignerCache>
{
    [ObservableProperty]
    public partial MicroscopeLensInformation LowMicroscopeLensInformation { get; set; } = MicroscopeLensInformation.Default;

    [ObservableProperty]
    public partial MicroscopeLensInformation HighMicroscopeLensInformation { get; set; } = MicroscopeLensInformation.Default;

    /// <summary>
    /// 晶圆类型
    /// </summary>
    [ObservableProperty]
    public partial AlgorithmWaferTypeEnum AlgorithmWaferTypeEnum { get; set; } = AlgorithmWaferTypeEnum.D300;

    [ObservableProperty]
    public partial AlgorithmTemplateSizeEnum LowSizeEnum { get; set; } = AlgorithmTemplateSizeEnum.Size256;

    [ObservableProperty]
    public partial AlgorithmTemplateSizeEnum HighSizeEnum { get; set; } = AlgorithmTemplateSizeEnum.Size256;

    [ObservableProperty]
    public partial double NccTypeTemplateMatchScoreThreshold { get; set; } = 0.8;

    /// <summary>
    /// 低倍率mark点1位置(wafer中间掩模版芯粒左上角)
    /// </summary>
    [ObservableProperty]
    public partial AlignmentSiteDto LowSite1 { get; set; } = new();

    /// <summary>
    /// 低倍率mark点2位置(mark点1的相邻掩模版芯粒左上角)[没有模板, 用低倍率mark点1模板匹配]
    /// </summary>
    [ObservableProperty]
    public partial AlignmentSiteDto LowSite2 { get; set; } = new();

    /// <summary>
    /// 高倍率mark点1位置(低倍率mark点1的精细位置)
    /// </summary>
    [ObservableProperty]
    public partial AlignmentSiteDto HighSite1 { get; set; } = new();

    /// <summary>
    /// 高倍率mark点2位置(低倍率mark点2的精细位置)[没有模板, 用高倍率mark点1模板匹配]
    /// </summary>
    [ObservableProperty]
    public partial AlignmentSiteDto HighSite2 { get; set; } = new();

    [ObservableProperty]
    public partial string LowSiteTemplateFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string HighSiteTemplateFilePath { get; set; } = string.Empty;

    [Comparison(0.1d, NumberComparisonTypeEnum.GreaterThan, ErrorMessage = "Wafer radius must be greater than 0.1.")]
    public double WaferRadius
    {
        get;
        set => SetProperty(ref field, value, true);
    } = 150_000;

    [Comparison(0.1d, NumberComparisonTypeEnum.GreaterThan, ErrorMessage = "Die Pitch Width must be greater than 0.1.")]
    public double DiePitchWidth
    {
        get;
        set => SetProperty(ref field, value, true);
    } = 5100;

    [Comparison(1, NumberComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "Reticle Reference Die Col Count must be greater than 1.")]
    public int ReticleDieCountX
    {
        get;
        set => SetProperty(ref field, value, true);
    } = 1;

    [ObservableProperty]
    public partial Point OffsetPosition { get; set; }

    [ObservableProperty]
    public partial double Degrees { get; set; }

    [ObservableProperty]
    public partial Point EfemLoadWaferStagePosition { get; set; }

    [ObservableProperty]
    public partial double EfemLoadWaferChuckAngle { get; set; }

    [ObservableProperty]
    public partial double TeachingDegreesThreshold { get; set; } = 0.3;

    [ObservableProperty]
    public partial double VerifyDegreesThreshold { get; set; } = 0.1;

    [ObservableProperty]
    public partial double TeachingPositionThreshold { get; set; } = 100;

    [ObservableProperty]
    public partial double VerifyPositionThreshold { get; set; } = 300;

    [ObservableProperty]
    public partial Point FindWaferCenterOffset1 { get; set; }

    [ObservableProperty]
    public partial Point FindWaferCenterOffset2 { get; set; }

    [ObservableProperty]
    public partial Point FindWaferCenterOffset3 { get; set; }

    [ObservableProperty]
    public partial Point FindWaferCenterOffset4 { get; set; }

    [ObservableProperty]
    public partial Point FindWaferCenterOffset5 { get; set; }

    [ObservableProperty]
    public partial Point FindWaferCenterOffset6 { get; set; }

    [ObservableProperty]
    public partial Point FindWaferCenterOffset7 { get; set; }

    [ObservableProperty]
    public partial Point FindWaferCenterOffset8 { get; set; }

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial byte[] WaferCenterThumb1 { get; set; } = [];

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial byte[] WaferCenterThumb2 { get; set; } = [];

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial byte[] WaferCenterThumb3 { get; set; } = [];

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial byte[] WaferCenterThumb4 { get; set; } = [];

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial byte[] WaferCenterThumb5 { get; set; } = [];

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial byte[] WaferCenterThumb6 { get; set; } = [];

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial byte[] WaferCenterThumb7 { get; set; } = [];

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial byte[] WaferCenterThumb8 { get; set; } = [];

    [ObservableProperty]
    public partial int Times { get; set; } = 10;

    public override ChuckPrealignerCache Clone() => new()
    {
        LowMicroscopeLensInformation = LowMicroscopeLensInformation.Clone(),
        HighMicroscopeLensInformation = HighMicroscopeLensInformation.Clone(),
        AlgorithmWaferTypeEnum = AlgorithmWaferTypeEnum,
        LowSizeEnum = LowSizeEnum,
        HighSizeEnum = HighSizeEnum,
        NccTypeTemplateMatchScoreThreshold = NccTypeTemplateMatchScoreThreshold,
        LowSite1 = LowSite1.Clone(),
        LowSite2 = LowSite2.Clone(),
        HighSite1 = HighSite1.Clone(),
        HighSite2 = HighSite2.Clone(),
        LowSiteTemplateFilePath = LowSiteTemplateFilePath,
        HighSiteTemplateFilePath = HighSiteTemplateFilePath,
        WaferRadius = WaferRadius,
        DiePitchWidth = DiePitchWidth,
        ReticleDieCountX = ReticleDieCountX,
        OffsetPosition = OffsetPosition,
        Degrees = Degrees,
        EfemLoadWaferStagePosition = EfemLoadWaferStagePosition,
        EfemLoadWaferChuckAngle = EfemLoadWaferChuckAngle,
        TeachingDegreesThreshold = TeachingDegreesThreshold,
        VerifyDegreesThreshold = VerifyDegreesThreshold,
        TeachingPositionThreshold = TeachingPositionThreshold,
        VerifyPositionThreshold = VerifyPositionThreshold,
        FindWaferCenterOffset1 = FindWaferCenterOffset1,
        FindWaferCenterOffset2 = FindWaferCenterOffset2,
        FindWaferCenterOffset3 = FindWaferCenterOffset3,
        FindWaferCenterOffset4 = FindWaferCenterOffset4,
        FindWaferCenterOffset5 = FindWaferCenterOffset5,
        FindWaferCenterOffset6 = FindWaferCenterOffset6,
        FindWaferCenterOffset7 = FindWaferCenterOffset7,
        FindWaferCenterOffset8 = FindWaferCenterOffset8,
        WaferCenterThumb1 = [.. WaferCenterThumb1],
        WaferCenterThumb2 = [.. WaferCenterThumb2],
        WaferCenterThumb3 = [.. WaferCenterThumb3],
        WaferCenterThumb4 = [.. WaferCenterThumb4],
        WaferCenterThumb5 = [.. WaferCenterThumb5],
        WaferCenterThumb6 = [.. WaferCenterThumb6],
        WaferCenterThumb7 = [.. WaferCenterThumb7],
        WaferCenterThumb8 = [.. WaferCenterThumb8],
        Times = Times,
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration,
    };
}
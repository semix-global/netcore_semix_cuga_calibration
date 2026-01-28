using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Algorithm;
using Core.Models.Enums.Stage;
using Core.Models.Models.Common.Alignment;
using Core.Models.Models.Common.Pattern;
using Local.NoSQL.DB.Providers.Bases;
using Net.Utilities.DataAnnotations;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Models.Enums.Maths;
using Net.Utilities.Models.Geometries;
using System.Collections.Concurrent;

namespace Core.Models.Models.Microscope.CalChip;

public sealed partial class MicroscopeCalChipCache : CalibrationCacheBase
{
    [ObservableProperty]
    private MicroscopeLensInformation _lowMicroscopeLensInformation = MicroscopeLensInformation.Default;

    [ObservableProperty]
    private MicroscopeLensInformation _highMicroscopeLensInformation = MicroscopeLensInformation.Default;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    private CalChipSiteModelEnum _calChipSiteModelEnum = CalChipSiteModelEnum.DswModel;

    [ObservableProperty]
    private bool _isSkipRtfc;

    [ObservableProperty]
    private MicroscopeCalChipCacheItem[] _microscopeCalChipCacheItems = [];

    public ConcurrentBag<KeyValuePair<CalChipSiteModelEnum, MicroscopeCalChipCacheItem>> Items { get; init; } = [];

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    [LiteDB.BsonIgnore]
    public MicroscopeCalChipCacheItem Item => Items.GetOrAdd(CalChipSiteModelEnum, new MicroscopeCalChipCacheItem { CalChipSiteModelEnum = CalChipSiteModelEnum });

    [ObservableProperty]
    private Point _chuckPosition = Point.Origin;

    /// <summary>
    /// 晶圆类型
    /// </summary>
    [ObservableProperty]
    private AlgorithmWaferTypeEnum _algorithmWaferTypeEnum = AlgorithmWaferTypeEnum.D300;

    [ObservableProperty]
    private AlgorithmTemplateSizeEnum _lowSizeEnum = AlgorithmTemplateSizeEnum.Size256;

    [ObservableProperty]
    private AlgorithmTemplateSizeEnum _highSizeEnum = AlgorithmTemplateSizeEnum.Size256;

    [ObservableProperty]
    private double _nccTypeTemplateMatchScoreThreshold = 0.8;

    /// <summary>
    /// 低倍率mark点1位置(wafer中间掩模版芯粒左上角)
    /// </summary>
    [ObservableProperty]
    private AlignmentSiteDto _lowSite1 = new();

    /// <summary>
    /// 低倍率mark点2位置(mark点1的相邻掩模版芯粒左上角)[没有模板, 用低倍率mark点1模板匹配]
    /// </summary>
    [ObservableProperty]
    private AlignmentSiteDto _lowSite2 = new();

    /// <summary>
    /// 高倍率mark点1位置(低倍率mark点1的精细位置)
    /// </summary>
    [ObservableProperty]
    private AlignmentSiteDto _highSite1 = new();

    /// <summary>
    /// 高倍率mark点2位置(低倍率mark点2的精细位置)[没有模板, 用高倍率mark点1模板匹配]
    /// </summary>
    [ObservableProperty]
    private AlignmentSiteDto _highSite2 = new();

    [ObservableProperty]
    private string _lowSiteTemplateFilePath = string.Empty;

    [ObservableProperty]
    private string _highSiteTemplateFilePath = string.Empty;

    [ObservableProperty]
    private double _dSWAlignmentVerifyThreshold;

    [ObservableProperty]
    private string _verifyBrightFieldResultError = string.Empty;

    [ObservableProperty]
    private string _verifyDarkFieldResultError = string.Empty;

    /// <summary>
    /// 校准 chuck、dsw、haze rtfc输出的af offset和校准结果的差值需小于该阈值 
    /// </summary>
    [ObservableProperty]
    private double _afMotorOffsetThreshold;

    /// <summary>
    /// BF verify清晰度得分和校准结果的清晰度差值需小于该阈值
    /// </summary>
    [Comparison(1d, NumberComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "BfQualityThreshold: ")]
    public double BfQualityThreshold
    {
        get;
        set => SetProperty(ref field, value, validate: true);
    } = 1;

    /// <summary>
    /// DF verify清晰度得分和校准结果的清晰度差值需小于该阈值
    /// </summary>
    [Comparison(1d, NumberComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "DfQualityThreshold: ")]
    public double DfQualityThreshold
    {
        get;
        set => SetProperty(ref field, value, validate: true);
    } = 1;
}

public sealed partial class MicroscopeCalChipCacheItem : ObservableCacheBase
{
    [ObservableProperty]
    private CalChipSiteModelEnum _calChipSiteModelEnum;

    [Comparison(1d, NumberComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "FindFocusMin: ")]
    public double FindFocusMin
    {
        get;
        set => SetProperty(ref field, value, validate: true);
    } = 1;

    [Comparison(1d, NumberComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "FindFocusMax: ")]
    public double FindFocusMax
    {
        get;
        set => SetProperty(ref field, value, validate: true);
    } = 1;

    [Comparison(1d, NumberComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "FindFocusInterval: ")]
    public double FindFocusInterval
    {
        get;
        set => SetProperty(ref field, value, validate: true);
    } = 1;

    #region Position

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CenterPosition))]
    private Point _leftTopPosition;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CenterPosition))]
    private Point _rightBottomPosition;

    public Point CenterPosition => (LeftTopPosition + (Vector)RightBottomPosition) / 2;

    #endregion Position
}
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Stage;
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
    private double _bfQualityThreshold = 1;
    private double _dfQualityThreshold = 1;

    [ObservableProperty]
    private MicroscopeLensInformation _microscopeLensInformation = MicroscopeLensInformation.Default;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    private CalChipSiteModelEnum _calChipSiteModelEnum = CalChipSiteModelEnum.DswModel;

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
        get => _bfQualityThreshold;
        set => SetProperty(ref _bfQualityThreshold, value, validate: true);
    }

    /// <summary>
    /// DF verify清晰度得分和校准结果的清晰度差值需小于该阈值
    /// </summary>
    [Comparison(1d, NumberComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "DfQualityThreshold: ")]
    public double DfQualityThreshold
    {
        get => _dfQualityThreshold;
        set => SetProperty(ref _dfQualityThreshold, value, validate: true);
    }
}

public sealed partial class MicroscopeCalChipCacheItem : ObservableCacheBase
{
    [ObservableProperty]
    private CalChipSiteModelEnum _calChipSiteModelEnum;

    private double _findFocusMin = 1;

    private double _findFocusMax = 1;

    private double _findFocusInterval = 1;

    [Comparison(1d, NumberComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "FindFocusMin: ")]
    public double FindFocusMin
    {
        get => _findFocusMin;
        set => SetProperty(ref _findFocusMin, value, validate: true);
    }

    [Comparison(1d, NumberComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "FindFocusMax: ")]
    public double FindFocusMax
    {
        get => _findFocusMax;
        set => SetProperty(ref _findFocusMax, value, validate: true);
    }

    [Comparison(1d, NumberComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "FindFocusInterval: ")]
    public double FindFocusInterval
    {
        get => _findFocusInterval;
        set => SetProperty(ref _findFocusInterval, value, validate: true);
    }

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
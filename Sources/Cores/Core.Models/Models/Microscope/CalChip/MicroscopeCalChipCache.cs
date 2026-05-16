using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Stage;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.DataAnnotations;
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

    [Newtonsoft.Json.JsonConverter(typeof(Net.Utilities.Models.Serializations.DictionaryConverter<CalChipSiteModelEnum, MicroscopeCalChipCacheItem>))]
    public ConcurrentDictionary<CalChipSiteModelEnum, MicroscopeCalChipCacheItem> Items { get; init; } = [];

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    public MicroscopeCalChipCacheItem Item => Items.GetOrAdd(CalChipSiteModelEnum, _ => new MicroscopeCalChipCacheItem { CalChipSiteModelEnum = CalChipSiteModelEnum });

    [ObservableProperty]
    private string _verifyQualityError = string.Empty;

    [ObservableProperty]
    private double _speedEcsPerSecond = 500;

    [ObservableProperty]
    private double _halfEcsLength = 250;

    /// <summary>
    /// BF verify清晰度得分和校准结果的清晰度差值需小于该阈值
    /// </summary>
    [Comparison(1d, NumberComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = $"{nameof(QualityThreshold)}: ")]
    public double QualityThreshold
    {
        get;
        set => SetProperty(ref field, value, validate: true);
    } = 1;
}

public sealed partial class MicroscopeCalChipCacheItem : ObservableValidator
{
    [ObservableProperty]
    private CalChipSiteModelEnum _calChipSiteModelEnum;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CenterMachinePosition))]
    private Point _leftTopMachinePosition;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CenterMachinePosition))]
    private Point _rightBottomMachinePosition;

    public Point CenterMachinePosition => (LeftTopMachinePosition + (Vector)RightBottomMachinePosition) / 2;
}
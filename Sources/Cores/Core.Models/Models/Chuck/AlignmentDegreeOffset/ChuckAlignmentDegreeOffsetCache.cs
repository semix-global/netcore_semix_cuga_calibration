using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Algorithm;
using Core.Models.Enums.Optics;
using Core.Models.Helper;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Helpers.Extensions;
using Newtonsoft.Json;
using System.Collections.Concurrent;

namespace Core.Models.Models.Chuck.AlignmentDegreeOffset;

public sealed partial class ChuckAlignmentDegreeOffsetCache : CalibrationCacheBase
{
    [ObservableProperty]
    private MicroscopeLensInformation _lowMicroscopeLensInformation = MicroscopeLensInformation.Default;

    [ObservableProperty]
    private MicroscopeLensInformation _highMicroscopeLensInformation = MicroscopeLensInformation.Default;

    /// <summary>
    /// 晶圆类型
    /// </summary>
    [ObservableProperty]
    private AlgorithmWaferTypeEnum _algorithmWaferTypeEnum = AlgorithmWaferTypeEnum.D300;

    [ObservableProperty]
    private OpticsIlluminationModeEnum _opticsIlluminationModeEnum = CalibrationConstantsHelper.MainOpticsIlluminationModeEnum;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    public ConcurrentBag<KeyValuePair<(OpticsIlluminationModeEnum, ProductivityInformation), ChuckAlignmentDegreeOffsetCacheItem>> Items { get; init; } = [];

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    public ChuckAlignmentDegreeOffsetCacheItem Item => Items.GetOrAdd((OpticsIlluminationModeEnum, ProductivityInformation), new Lazy<ChuckAlignmentDegreeOffsetCacheItem>(() => new ChuckAlignmentDegreeOffsetCacheItem()));

    [ObservableProperty]
    private double _nccTypeTemplateMatchScoreThreshold = 0.8;

    [ObservableProperty]
    private double _teachingThreshold;

    [ObservableProperty]
    private double _verifyThreshold;
}

public sealed partial class ChuckAlignmentDegreeOffsetCacheItem : CalibrationCacheBase
{
    [ObservableProperty]
    private int _xWidthPixel = 800;
}
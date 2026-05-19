using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Algorithm;
using Core.Models.Enums.Recipe.Wafer;
using Core.Models.Enums.Stage;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Models.Geometries;
using System.Collections.Concurrent;

namespace Core.Models.Models.Microscope.PixelSize;

public sealed partial class MicroscopePixelSizeCache : CalibrationCacheBase<MicroscopePixelSizeCache>
{
    [ObservableProperty]
    public partial CalChipSiteModelEnum CalChipSiteModelEnum { get; set; } = CalChipSiteModelEnum.DswModel;

    [ObservableProperty]
    public partial MicroscopeLensInformation MicroscopeLensInformation { get; set; } = MicroscopeLensInformation.Default;

    /// <summary>
    /// 网格水平角度阈值
    /// </summary>
    [ObservableProperty]
    public partial double AngleThreshold { get; set; }

    [ObservableProperty]
    public partial Size Threshold { get; set; }

    [ObservableProperty]
    public partial ConcurrentDictionary<string, MicroscopePixelSizeCacheItem> MicroscopePixelSizeCacheItemDic { get; set; } = [];

    [Newtonsoft.Json.JsonIgnore]
    public MicroscopePixelSizeCacheItem CurrentCalibrationCacheItem => MicroscopePixelSizeCacheItemDic.GetOrAdd(MicroscopeLensInformation.LensName, new MicroscopePixelSizeCacheItem { LensInformation = MicroscopeLensInformation.Clone() });

    public void SetFindFocusPosition(Point position)
    {
        CurrentCalibrationCacheItem.FindPosition = position;
    }

    public override MicroscopePixelSizeCache Clone() => new()
    {
        CalChipSiteModelEnum = CalChipSiteModelEnum,
        MicroscopeLensInformation = MicroscopeLensInformation.Clone(),
        AngleThreshold = AngleThreshold,
        Threshold = Threshold,
        MicroscopePixelSizeCacheItemDic = new ConcurrentDictionary<string, MicroscopePixelSizeCacheItem>(MicroscopePixelSizeCacheItemDic.Select(t => new KeyValuePair<string, MicroscopePixelSizeCacheItem>(t.Key, t.Value.Clone()))),
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration
    };
}

public sealed partial class MicroscopePixelSizeCacheItem : CalibrationCacheBase<MicroscopePixelSizeCacheItem>
{
    [ObservableProperty]
    public partial MicroscopeLensInformation LensInformation { get; set; } = MicroscopeLensInformation.Default;

    [ObservableProperty]
    public partial WaferMaskTypeEnum WaferMaskTypeEnum { get; set; } = WaferMaskTypeEnum.Grid_100um;

    [ObservableProperty]
    public partial AlgorithmStandardMaskSquareSizeEnum AlgorithmStandardMaskSquareSizeEnum { get; set; } = AlgorithmStandardMaskSquareSizeEnum.Size10;

    [ObservableProperty]
    public partial Point FindPosition { get; set; }

    public override MicroscopePixelSizeCacheItem Clone() => new()
    {
        LensInformation = LensInformation.Clone(),
        WaferMaskTypeEnum = WaferMaskTypeEnum,
        AlgorithmStandardMaskSquareSizeEnum = AlgorithmStandardMaskSquareSizeEnum,
        FindPosition = FindPosition,
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration
    };
}
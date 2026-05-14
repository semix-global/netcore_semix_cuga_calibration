using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Stage;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Models.Geometries;
using System.Collections.Concurrent;
using Core.Models.Enums.Recipe.Wafer;

namespace Core.Models.Models.Microscope.Centricity;

public sealed partial class MicroscopeCentricityCache : CalibrationCacheBase<MicroscopeCentricityCache>
{
    [ObservableProperty]
    public partial MicroscopeLensInformation MicroscopeLensInformation { get; set; } = MicroscopeLensInformation.Default;

    [ObservableProperty]
    public partial CalChipSiteModelEnum CalChipSiteModelEnum { get; set; } = CalChipSiteModelEnum.DswModel;

    [ObservableProperty]
    public partial ConcurrentDictionary<string, MicroscopeCentricityCacheItem> MicroscopeCentricityCacheItemDic { get; set; } = [];

    [Newtonsoft.Json.JsonIgnore]
    public MicroscopeCentricityCacheItem CurrentCalibrationCacheItem => MicroscopeCentricityCacheItemDic.GetOrAdd(MicroscopeLensInformation.LensName, new MicroscopeCentricityCacheItem { LensInformation = MicroscopeLensInformation.Clone() });

    [ObservableProperty]
    public partial Point VerifyResultPosition { get; set; }

    [ObservableProperty]
    public partial Point VerifyResultError { get; set; }

    [ObservableProperty]
    public partial Point Threshold { get; set; }

    [ObservableProperty]
    public partial double ConcentricThreshold { get; set; }

    public void SetFindPosition(Point position)
    {
        CurrentCalibrationCacheItem.FindPosition = position;
    }

    public void SetTemplateFilePath(string templateFilePath)
    {
        CurrentCalibrationCacheItem.TemplateFilePath = templateFilePath;
    }

    public void SetTemplateImageFilePath(string templateImageFilePath)
    {
        CurrentCalibrationCacheItem.TemplateImageFilePath = templateImageFilePath;
    }

    public override MicroscopeCentricityCache Clone() => new()
    {
        MicroscopeLensInformation = MicroscopeLensInformation.Clone(),
        CalChipSiteModelEnum = CalChipSiteModelEnum,
        MicroscopeCentricityCacheItemDic = new ConcurrentDictionary<string, MicroscopeCentricityCacheItem>(MicroscopeCentricityCacheItemDic.Select(t => new KeyValuePair<string, MicroscopeCentricityCacheItem>(t.Key, t.Value.Clone()))),
        VerifyResultPosition = VerifyResultPosition,
        VerifyResultError = VerifyResultError,
        Threshold = Threshold,
        ConcentricThreshold = ConcentricThreshold,
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration
    };
}

public sealed partial class MicroscopeCentricityCacheItem : CalibrationCacheBase<MicroscopeCentricityCacheItem>
{
    [ObservableProperty]
    public partial MicroscopeLensInformation LensInformation { get; set; } = MicroscopeLensInformation.Default;

    [ObservableProperty]
    public partial WaferMaskTypeEnum WaferMaskTypeEnum { get; set; } = WaferMaskTypeEnum.DieCorner;

    [ObservableProperty]
    public partial Point FindPosition { get; set; }

    [ObservableProperty]
    public partial string TemplateFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string TemplateImageFilePath { get; set; } = string.Empty;

    public override MicroscopeCentricityCacheItem Clone() => new()
    {
        LensInformation = LensInformation.Clone(),
        WaferMaskTypeEnum = WaferMaskTypeEnum,
        FindPosition = FindPosition,
        TemplateFilePath = TemplateFilePath,
        TemplateImageFilePath = TemplateImageFilePath,
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration
    };
}
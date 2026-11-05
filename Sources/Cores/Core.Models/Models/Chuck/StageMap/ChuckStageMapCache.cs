using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Alignment;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Common.StageMap;
using Core.Models.Models.Setting;
using Local.SQL.Cache.Providers.Bases;

namespace Core.Models.Models.Chuck.StageMap;

[CacheVersion("2.0.0")]
public sealed partial class ChuckStageMapCache : CalibrationCacheBase<ChuckStageMapCache>
{
    [ObservableProperty]
    public partial MicroscopeLensInformation MicroscopeLensInformation { get; set; } = MicroscopeLensInformation.Default;

    [ObservableProperty]
    public partial ChuckStageMapCacheBFItem ChuckStageMapCacheBFItem { get; set; } = new();

    [ObservableProperty]
    public partial ChuckStageMapCacheDFItem ChuckStageMapCacheDFItem { get; set; } = new();

    public override ChuckStageMapCache Clone() => new()
    {
        MicroscopeLensInformation = MicroscopeLensInformation.Clone(),
        ChuckStageMapCacheBFItem = ChuckStageMapCacheBFItem.Clone(),
        ChuckStageMapCacheDFItem = ChuckStageMapCacheDFItem.Clone(),
        Id = Id,
        Expiration = Expiration
    };
}

public sealed partial class ChuckStageMapCacheBFItem : CalibrationCacheBase<ChuckStageMapCacheBFItem>
{
    #region Common

    [ObservableProperty]
    public partial TemplateMatchSetting TemplateMatchSetting { get; set; } = new();

    #endregion

    [ObservableProperty]
    public partial int ImageWidth { get; set; } = 1000;

    [ObservableProperty]
    public partial double WaferRadius { get; set; } = 140_000d;

    [ObservableProperty]
    public partial double DiePitchWidth { get; set; } = 5100d;

    [ObservableProperty]
    public partial double DiePitchHeight { get; set; } = 16600d;

    [ObservableProperty]
    public partial StageMapTemplate[] StageMapTemplates { get; set; } = [];

    [ObservableProperty]
    public partial AlignmentResultDto AlignmentResult { get; set; } = new();

    partial void OnStageMapTemplatesChanged(StageMapTemplate[] value)
    {
        foreach (var (index, item) in value.Index()) item.DisplayIndex = index + 1;
    }

    public override ChuckStageMapCacheBFItem Clone() => new()
    {
        TemplateMatchSetting = TemplateMatchSetting.Clone(),
        ImageWidth = ImageWidth,
        WaferRadius = WaferRadius,
        DiePitchWidth = DiePitchWidth,
        DiePitchHeight = DiePitchHeight,
        StageMapTemplates = [.. StageMapTemplates.Select(x => x.Clone())],
        AlignmentResult = AlignmentResult.Clone(),
        Id = Id,
        Expiration = Expiration
    };
}

public sealed partial class ChuckStageMapCacheDFItem : CalibrationCacheBase<ChuckStageMapCacheDFItem>
{
    #region Common

    [ObservableProperty]
    public partial ProductivityInformation ProductivityInformation { get; set; } = ProductivityInformation.Default;

    [ObservableProperty]
    public partial LaserLightInformation LaserLightInformation { get; set; } = LaserLightInformation.Default;

    [ObservableProperty]
    public partial CIBInformation CIBInformation { get; set; } = CIBInformation.Default;

    [ObservableProperty]
    public partial OpticsConfiguration OpticsConfiguration { get; set; } = new();

    [ObservableProperty]
    public partial CIBConfiguration CIBConfiguration { get; set; } = new();

    [ObservableProperty]
    public partial TemplateMatchSetting TemplateMatchSetting { get; set; } = new();

    #endregion

    [ObservableProperty]
    public partial int ImageWidth { get; set; } = 1000;

    [ObservableProperty]
    public partial double WaferRadius { get; set; } = 140_000d;

    [ObservableProperty]
    public partial double DiePitchWidth { get; set; } = 5100d;

    [ObservableProperty]
    public partial double DiePitchHeight { get; set; } = 16600d;

    [ObservableProperty]
    public partial StageMapTemplate[] StageMapTemplates { get; set; } = [];

    [ObservableProperty]
    public partial int StageMapRepeatTimes { get; set; } = 10;

    [ObservableProperty]
    public partial bool IsDarkFieldAlignment { get; set; }

    [ObservableProperty]
    public partial AlignmentResultDto AlignmentResult { get; set; } = new();

    partial void OnStageMapTemplatesChanged(StageMapTemplate[] value)
    {
        foreach (var (index, item) in value.Index()) item.DisplayIndex = index + 1;
    }

    public override ChuckStageMapCacheDFItem Clone() => new()
    {
        ProductivityInformation = ProductivityInformation.Clone(),
        LaserLightInformation = LaserLightInformation.Clone(),
        CIBInformation = CIBInformation.Clone(),
        OpticsConfiguration = OpticsConfiguration.Clone(),
        CIBConfiguration = CIBConfiguration.Clone(),
        TemplateMatchSetting = TemplateMatchSetting.Clone(),
        ImageWidth = ImageWidth,
        WaferRadius = WaferRadius,
        DiePitchWidth = DiePitchWidth,
        DiePitchHeight = DiePitchHeight,
        StageMapTemplates = [.. StageMapTemplates.Select(x => x.Clone())],
        StageMapRepeatTimes = StageMapRepeatTimes,
        IsDarkFieldAlignment = IsDarkFieldAlignment,
        AlignmentResult = AlignmentResult.Clone(),
        Id = Id,
        Expiration = Expiration
    };
}
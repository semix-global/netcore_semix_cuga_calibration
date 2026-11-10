using System.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Common.StageMap;
using Core.Models.Models.Setting;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Chuck.StageMap;

[CacheVersion("2.0.0")]
public sealed partial class ChuckStageMapCache : CalibrationCacheBase<ChuckStageMapCache>
{
    [ObservableProperty]
    public partial MicroscopeLensInformation MicroscopeLensInformation { get; set; } = MicroscopeLensInformation.Default;

    [ObservableProperty]
    public partial ChuckStageMapCacheBFItem ChuckStageMapCacheBFItem { get; set; } = new();

    [ObservableProperty]
    public partial ChuckStageMapCacheDFItem ChuckStageMapCacheDFItem { get; set; } = new() { DiePitchWidth = 5100d };

    [ObservableProperty]
    public partial double Threshold { get; set; }

    public override ChuckStageMapCache Clone() => new()
    {
        MicroscopeLensInformation = MicroscopeLensInformation.Clone(),
        ChuckStageMapCacheBFItem = ChuckStageMapCacheBFItem.Clone(),
        ChuckStageMapCacheDFItem = ChuckStageMapCacheDFItem.Clone(),
        Threshold = Threshold,
        Id = Id,
        Expiration = Expiration
    };
}

public partial class ChuckStageMapCacheBFItem : CalibrationCacheBase<ChuckStageMapCacheBFItem>
{
    [ObservableProperty]
    public partial TemplateMatchSetting TemplateMatchSetting { get; set; } = new();

    [ObservableProperty]
    public partial double WaferRadius { get; set; } = 140_000d;

    [ObservableProperty]
    public partial double DiePitchWidth { get; set; } = 15300d;

    [ObservableProperty]
    public partial double DiePitchHeight { get; set; } = 16600d;

    [ObservableProperty]
    public partial StageMapTemplate[] StageMapTemplates { get; set; } = [];

    partial void OnStageMapTemplatesChanged(StageMapTemplate[] value)
    {
        foreach (var (index, item) in value.Index()) item.DisplayIndex = index + 1;
    }

    [RelayCommand]
    private void RemoveStageMapTemplates(IEnumerable? selectedItems)
    {
        if (selectedItems is null) return;

        var stageMapTemplates = StageMapTemplates.ToList();

        foreach (StageMapTemplate selectedItem in selectedItems) stageMapTemplates.Remove(selectedItem);

        StageMapTemplates = [.. stageMapTemplates];
    }

    protected virtual ChuckStageMapCacheBFItem CreateInstance() => new();

    public override ChuckStageMapCacheBFItem Clone()
    {
        var instance = CreateInstance();

        instance.TemplateMatchSetting = TemplateMatchSetting.Clone();
        instance.WaferRadius = WaferRadius;
        instance.DiePitchWidth = DiePitchWidth;
        instance.DiePitchHeight = DiePitchHeight;
        instance.StageMapTemplates = [.. StageMapTemplates.Select(x => x.Clone())];
        instance.Id = Id;
        instance.Expiration = Expiration;

        return instance;
    }
}

public sealed partial class ChuckStageMapCacheDFItem : ChuckStageMapCacheBFItem, ICloneable<ChuckStageMapCacheDFItem>
{
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
    public partial int ImageWidth { get; set; } = 3000;

    [ObservableProperty]
    public partial int StageMapRepeatTimes { get; set; } = 2;

    protected override ChuckStageMapCacheBFItem CreateInstance() => new ChuckStageMapCacheDFItem();

    public new ChuckStageMapCacheDFItem Clone()
    {
        var clone = (ChuckStageMapCacheDFItem)base.Clone();

        clone.ProductivityInformation = ProductivityInformation.Clone();
        clone.LaserLightInformation = LaserLightInformation.Clone();
        clone.CIBInformation = CIBInformation.Clone();
        clone.OpticsConfiguration = OpticsConfiguration.Clone();
        clone.CIBConfiguration = CIBConfiguration.Clone();
        clone.ImageWidth = ImageWidth;
        clone.StageMapRepeatTimes = StageMapRepeatTimes;

        return clone;
    }
}
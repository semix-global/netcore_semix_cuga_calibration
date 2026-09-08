using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Stage;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Models.Serializations;
using System.Collections.Concurrent;

namespace Core.Models.Models.Optics.ROOS;

public sealed partial class OpticsROOSCache : CalibrationCacheBase<OpticsROOSCache>
{
    [ObservableProperty]
    public partial MicroscopeLensInformation MicroscopeLensInformation { get; set; } = MicroscopeLensInformation.Default;

    [ObservableProperty]
    public partial CalChipSiteModelEnum CalChipSiteModelEnum { get; set; } = CalChipSiteModelEnum.HazeModel;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    public partial ProductivityInformation ProductivityInformation { get; set; } = ProductivityInformation.Default;

    [Newtonsoft.Json.JsonConverter(typeof(DictionaryConverter<ProductivityInformation, OpticsROOSCacheItem>))]
    public ConcurrentDictionary<ProductivityInformation, OpticsROOSCacheItem> Items { get; init; } = [];

    [Newtonsoft.Json.JsonIgnore]
    public OpticsROOSCacheItem Item => Items.GetOrAdd(ProductivityInformation, _ => new OpticsROOSCacheItem());

    [ObservableProperty]
    public partial double VerifyThreshold { get; set; }

    public override OpticsROOSCache Clone() => new()
    {
        MicroscopeLensInformation = MicroscopeLensInformation,
        CalChipSiteModelEnum = CalChipSiteModelEnum,
        ProductivityInformation = ProductivityInformation,
        Items = new ConcurrentDictionary<ProductivityInformation, OpticsROOSCacheItem>(Items.Select(t => new KeyValuePair<ProductivityInformation, OpticsROOSCacheItem>(t.Key, t.Value.Clone()))),
        Id = Id,
        Expiration = Expiration
    };
}

public sealed partial class OpticsROOSCacheItem : CalibrationCacheBase<OpticsROOSCacheItem>
{
    [ObservableProperty]
    public partial LaserLightInformation LaserLightInformation { get; set; } = LaserLightInformation.Default;

    [ObservableProperty]
    public partial CIBInformation CIBInformation { get; set; } = CIBInformation.Default;

    [ObservableProperty]
    public partial CIBConfiguration CIBConfiguration { get; set; } = new();

    [ObservableProperty]
    public partial OpticsConfiguration OpticsConfiguration { get; set; } = new();

    [ObservableProperty]
    public partial Point FindBFMachinePosition { get; set; }

    #region Image Param

    [ObservableProperty]
    public partial int ImageWidth { get; set; } = 1000;

    [ObservableProperty]
    public partial double ConfigImageYPixelHeight { get; set; }

    [ObservableProperty]
    public partial double ConfigCropImageStartYPixel { get; set; }

    [ObservableProperty]
    public partial double ConfigCropImageEndYPixel { get; set; }

    [ObservableProperty]
    public partial double IdealImageYUmHeight { get; set; }

    [ObservableProperty]
    public partial double IdealImageYPixelHeight { get; set; }

    #endregion

    #region ROOS Motor

    [ObservableProperty]
    public partial double CurrentConfigROOSPos { get; set; }

    [ObservableProperty]
    public partial double StartROOSPos { get; set; }

    [ObservableProperty]
    public partial double StepROOSPos { get; set; }

    [ObservableProperty]
    public partial double StopROOSPos { get; set; }

    #endregion

    [ObservableProperty]
    public partial double ROOSAlignOffsetThreshold { get; set; } = 1d;

    public override OpticsROOSCacheItem Clone() => new()
    {
        LaserLightInformation = LaserLightInformation,
        CIBInformation = CIBInformation,
        CIBConfiguration = CIBConfiguration,
        OpticsConfiguration = OpticsConfiguration,
        FindBFMachinePosition = FindBFMachinePosition,
        ImageWidth = ImageWidth,
        ConfigImageYPixelHeight = ConfigImageYPixelHeight,
        ConfigCropImageStartYPixel = ConfigCropImageStartYPixel,
        ConfigCropImageEndYPixel = ConfigCropImageEndYPixel,
        IdealImageYUmHeight = IdealImageYUmHeight,
        IdealImageYPixelHeight = IdealImageYPixelHeight,
        CurrentConfigROOSPos = CurrentConfigROOSPos,
        StartROOSPos = StartROOSPos,
        StepROOSPos = StepROOSPos,
        StopROOSPos = StopROOSPos,
        ROOSAlignOffsetThreshold = ROOSAlignOffsetThreshold,
        Id = Id,
        Expiration = Expiration
    };
}
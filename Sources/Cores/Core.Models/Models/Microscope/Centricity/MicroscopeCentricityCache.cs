using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Stage;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Models.Geometries;
using System.Collections.Concurrent;

namespace Core.Models.Models.Microscope.Centricity;

public sealed partial class MicroscopeCentricityCache : CalibrationCacheBase
{
    [ObservableProperty]
    private MicroscopeLensInformation _microscopeLensInformation = MicroscopeLensInformation.Default;

    [ObservableProperty]
    private CalChipSiteModelEnum _calChipSiteModelEnum = CalChipSiteModelEnum.DswModel;

    [ObservableProperty]
    private ConcurrentDictionary<string, MicroscopeCentricityCacheItem> _microscopeCentricityCacheItemDic = [];

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    [LiteDB.BsonIgnore]
    public MicroscopeCentricityCacheItem CurrentCalibrationCacheItem =>
        MicroscopeCentricityCacheItemDic.GetOrAdd(MicroscopeLensInformation.LensName, new MicroscopeCentricityCacheItem { LensInformation = MicroscopeLensInformation.Clone() });

    [ObservableProperty]
    private Point _verifyResultPosition;

    [ObservableProperty]
    private Point _verifyResultError;

    [ObservableProperty]
    private Point _threshold;

    [ObservableProperty]
    private double _concentricThreshold;

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

    public Point GetFindPosition(MicroscopeLensInformation lensInformation)
    {
        return CurrentCalibrationCacheItem.FindPosition;
    }

    public string GetTemplateFilePath(MicroscopeLensInformation lensInformation)
    {
        return CurrentCalibrationCacheItem.TemplateFilePath;
    }

    public string SetTemplateImageFilePath(MicroscopeLensInformation lensInformation)
    {
        return CurrentCalibrationCacheItem.TemplateImageFilePath;
    }
}
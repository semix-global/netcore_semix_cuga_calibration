using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Pattern;
using LiteDB;
using Net.Utilities.Models.Geometries;
using System.Collections.Concurrent;

namespace Core.Models.Models.Microscope.Centricity;

public sealed partial class MicroscopeCentricityCache : CalibrationCacheBase
{
    [ObservableProperty]
    private MicroscopeLensInformation _microscopeLensInformation = MicroscopeLensInformation.Default;

    [ObservableProperty]
    private ConcurrentDictionary<string, MicroscopeCentricityCacheItem> _microscopeCentricityCacheItemDic = [];

    [BsonIgnore]
    public MicroscopeCentricityCacheItem CurrentCalibrationCacheItem =>
        MicroscopeCentricityCacheItemDic.GetOrAdd(MicroscopeLensInformation.LensName, new MicroscopeCentricityCacheItem() { LensInformation = MicroscopeLensInformation.Clone() });

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
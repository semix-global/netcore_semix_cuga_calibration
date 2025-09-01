using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Pattern;
using MoreLinq;
using Net.Utilities.Models.Geometries;
using System.Collections.ObjectModel;

namespace Core.Models.Models.Microscope.Centricity;

public sealed partial class MicroscopeCentricityCache : CalibrationCacheBase
{
    [ObservableProperty]
    private MicroscopeLensInformation _microscopeLensInformation =  MicroscopeLensInformation.Default;

    [ObservableProperty]
    private ObservableCollection<MicroscopeCentricityCacheItem> _microscopeCentricityCacheItem = [];

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
        var info = MicroscopeCentricityCacheItem.SingleOrDefault(item => item.LensInformation == MicroscopeLensInformation) ?? throw new ArgumentNullException(nameof(SetFindPosition));
        info.FindPosition = position;
    }

    public void SetTemplateFilePath(string templateFilePath)
    {
        var info = MicroscopeCentricityCacheItem.SingleOrDefault(item => item.LensInformation == MicroscopeLensInformation) ?? throw new ArgumentNullException(nameof(SetTemplateFilePath));
        info.TemplateFilePath = templateFilePath;
    }

    public void SetTemplateImageFilePath(string templateImageFilePath)
    {
        var info = MicroscopeCentricityCacheItem.SingleOrDefault(item => item.LensInformation == MicroscopeLensInformation) ?? throw new ArgumentNullException(nameof(SetTemplateImageFilePath));
        info.TemplateImageFilePath = templateImageFilePath;
    }

    public Point GetFindPosition(MicroscopeLensInformation lensInformation)
    {
        var info = MicroscopeCentricityCacheItem.SingleOrDefault(item => item.LensInformation == lensInformation) ?? throw new ArgumentNullException(nameof(SetFindPosition));
        return info.FindPosition;
    }

    public string GetTemplateFilePath(MicroscopeLensInformation lensInformation)
    {
        var info = MicroscopeCentricityCacheItem.SingleOrDefault(item => item.LensInformation == lensInformation) ?? throw new ArgumentNullException(nameof(SetTemplateFilePath));
        return info.TemplateFilePath;
    }

    public string SetTemplateImageFilePath(MicroscopeLensInformation lensInformation)
    {
        var info = MicroscopeCentricityCacheItem.SingleOrDefault(item => item.LensInformation == lensInformation) ?? throw new ArgumentNullException(nameof(SetTemplateImageFilePath));
        return info.TemplateImageFilePath;
    }

    public MicroscopeCentricityCacheItem GetSelectedCacheItem()
    {
        return MicroscopeCentricityCacheItem.SingleOrDefault(item => item.LensInformation == MicroscopeLensInformation) ?? throw new ArgumentNullException(nameof(GetSelectedCacheItem));
    }

    public bool InitializeCacheList(List<MicroscopeLensInformation> microscopeLensInformationList)
    {
        if (microscopeLensInformationList.Count == 0) return false;
        var isInitialized = MicroscopeCentricityCacheItem.Count == microscopeLensInformationList.Count
                            && MicroscopeCentricityCacheItem.Select((item, index) => (index, item))
                                .All(t => t.item.LensInformation == microscopeLensInformationList[t.index]);
        if (isInitialized) return true;
        MicroscopeCentricityCacheItem = new ObservableCollection<MicroscopeCentricityCacheItem>(
            microscopeLensInformationList.Select(t => new MicroscopeCentricityCacheItem { LensInformation = t.Clone() }));
        MicroscopeLensInformation = MicroscopeCentricityCacheItem.Minima(t => t.LensInformation.LensCode).Single().LensInformation;
        return true;
    }
}
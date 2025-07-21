using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Pattern;
using MoreLinq;
using Net.Utilities.Models.Geometries;
using System.Collections.ObjectModel;

namespace Core.Models.Models.Microscope.Centricity;

public sealed partial class MicroscopeCentricityCache : CalibrationCacheBase
{
    [ObservableProperty]
    private MicroscopeMagnificationInfo _microscopeMagnificationInfo = new();

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
        var info = MicroscopeCentricityCacheItem.SingleOrDefault(item => item.MagnificationInfo == MicroscopeMagnificationInfo) ?? throw new ArgumentNullException(nameof(SetFindPosition));
        info.FindPosition = position;
    }

    public void SetTemplateFilePath(string templateFilePath)
    {
        var info = MicroscopeCentricityCacheItem.SingleOrDefault(item => item.MagnificationInfo == MicroscopeMagnificationInfo) ?? throw new ArgumentNullException(nameof(SetTemplateFilePath));
        info.TemplateFilePath = templateFilePath;
    }

    public void SetTemplateImageFilePath(string templateImageFilePath)
    {
        var info = MicroscopeCentricityCacheItem.SingleOrDefault(item => item.MagnificationInfo == MicroscopeMagnificationInfo) ?? throw new ArgumentNullException(nameof(SetTemplateImageFilePath));
        info.TemplateImageFilePath = templateImageFilePath;
    }

    public Point GetFindPosition(MicroscopeMagnificationInfo magnificationInfo)
    {
        var info = MicroscopeCentricityCacheItem.SingleOrDefault(item => item.MagnificationInfo == magnificationInfo) ?? throw new ArgumentNullException(nameof(SetFindPosition));
        return info.FindPosition;
    }

    public string GetTemplateFilePath(MicroscopeMagnificationInfo magnificationInfo)
    {
        var info = MicroscopeCentricityCacheItem.SingleOrDefault(item => item.MagnificationInfo == magnificationInfo) ?? throw new ArgumentNullException(nameof(SetTemplateFilePath));
        return info.TemplateFilePath;
    }

    public string SetTemplateImageFilePath(MicroscopeMagnificationInfo magnificationInfo)
    {
        var info = MicroscopeCentricityCacheItem.SingleOrDefault(item => item.MagnificationInfo == magnificationInfo) ?? throw new ArgumentNullException(nameof(SetTemplateImageFilePath));
        return info.TemplateImageFilePath;
    }

    public MicroscopeCentricityCacheItem GetSelectedCacheItem()
    {
        return MicroscopeCentricityCacheItem.SingleOrDefault(item => item.MagnificationInfo == MicroscopeMagnificationInfo) ?? throw new ArgumentNullException(nameof(GetSelectedCacheItem));
    }

    public bool InitializeCacheList(List<MicroscopeMagnificationInfo> microscopeMagnificationInfoList)
    {
        if (microscopeMagnificationInfoList.Count == 0) return false;
        var isInitialized = MicroscopeCentricityCacheItem.Count == microscopeMagnificationInfoList.Count
                            && MicroscopeCentricityCacheItem.Select((item, index) => (index, item))
                                .All(t => t.item.MagnificationInfo == microscopeMagnificationInfoList[t.index]);
        if (isInitialized) return true;
        MicroscopeCentricityCacheItem = new ObservableCollection<MicroscopeCentricityCacheItem>(
            microscopeMagnificationInfoList.Select(t => new MicroscopeCentricityCacheItem() { MagnificationInfo = t.Clone() }));
        MicroscopeMagnificationInfo = MicroscopeCentricityCacheItem.Minima(t => t.MagnificationInfo.MagnificationCode).Single().MagnificationInfo;
        return true;
    }
}
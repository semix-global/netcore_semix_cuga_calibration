using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Pattern;
using MoreLinq;
using Net.Utilities.DataAnnotations;
using Net.Utilities.Models.Enums.Maths;
using Net.Utilities.Models.Geometries;
using System.Collections.ObjectModel;

namespace Core.Models.Models.Microscope.PixelSize;

public sealed partial class MicroscopePixelSizeCache : CalibrationCacheBase
{
    [ObservableProperty]
    private MicroscopeMagnificationInfo _microscopeMagnificationInfo = new();

    [ObservableProperty]
    [Comparison(100000d, NumberComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "Chuck Radius: ")]
    private double _chuckRadius = 150000;

    /// <summary>
    /// 网格水平角度阈值
    /// </summary>
    [ObservableProperty]
    private double _angleThreshold;

    [ObservableProperty]
    private Size _threshold;

    [ObservableProperty]
    private ObservableCollection<MicroscopePixelSizeCacheItem> _microscopePixelSizeCacheItem = [];

    public void SetFindFocusPosition(Point position)
    {
        var info = MicroscopePixelSizeCacheItem.SingleOrDefault(item => item.MagnificationInfo == MicroscopeMagnificationInfo) ?? throw new ArgumentNullException(nameof(SetFindFocusPosition));
        info.FindPosition = position;
    }

    public MicroscopePixelSizeCacheItem GetSelectedCacheItem()
    {
        return MicroscopePixelSizeCacheItem.SingleOrDefault(item => item.MagnificationInfo == MicroscopeMagnificationInfo) ?? throw new ArgumentNullException(nameof(GetSelectedCacheItem));
    }

    public bool InitializeCacheList(List<MicroscopeMagnificationInfo> microscopeMagnificationInfoList)
    {
        if (microscopeMagnificationInfoList.Count == 0) return false;
        var isInitialized = MicroscopePixelSizeCacheItem.Count == microscopeMagnificationInfoList.Count
                            && MicroscopePixelSizeCacheItem.Select((item, index) => (index, item))
                                .All(t => t.item.MagnificationInfo == microscopeMagnificationInfoList[t.index]);
        if (isInitialized) return true;
        MicroscopePixelSizeCacheItem = new ObservableCollection<MicroscopePixelSizeCacheItem>(
            microscopeMagnificationInfoList.Select(t => new MicroscopePixelSizeCacheItem() { MagnificationInfo = t.Clone() }));
        MicroscopeMagnificationInfo = MicroscopePixelSizeCacheItem.Minima(t => t.MagnificationInfo.MagnificationCode).Single().MagnificationInfo;
        return true;
    }
}
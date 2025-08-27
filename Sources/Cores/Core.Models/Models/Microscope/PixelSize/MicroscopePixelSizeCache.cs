using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Pattern;
using MoreLinq;
using Net.Utilities.DataAnnotations;
using Net.Utilities.Models.Enums.Maths;
using Net.Utilities.Models.Geometries;
using System.Collections.ObjectModel;

namespace Core.Models.Models.Microscope.PixelSize;

public sealed partial class MicroscopePixelSizeCache : CalibrationCacheBase
{
    [ObservableProperty]
    private MicroscopeLensInformation _microscopeLensInformation = new();

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
        var info = MicroscopePixelSizeCacheItem.SingleOrDefault(item => item.LensInformation == MicroscopeLensInformation) ?? throw new ArgumentNullException(nameof(SetFindFocusPosition));
        info.FindPosition = position;
    }

    public MicroscopePixelSizeCacheItem GetSelectedCacheItem()
    {
        return MicroscopePixelSizeCacheItem.SingleOrDefault(item => item.LensInformation == MicroscopeLensInformation) ?? throw new ArgumentNullException(nameof(GetSelectedCacheItem));
    }

    public bool InitializeCacheList(List<MicroscopeLensInformation> microscopeLensInformationList)
    {
        if (microscopeLensInformationList.Count == 0) return false;
        var isInitialized = MicroscopePixelSizeCacheItem.Count == microscopeLensInformationList.Count
                            && MicroscopePixelSizeCacheItem.Select((item, index) => (index, item))
                                .All(t => t.item.LensInformation == microscopeLensInformationList[t.index]);
        if (isInitialized) return true;
        MicroscopePixelSizeCacheItem = new ObservableCollection<MicroscopePixelSizeCacheItem>(
            microscopeLensInformationList.Select(t => new MicroscopePixelSizeCacheItem() { LensInformation = t.Clone() }));
        MicroscopeLensInformation = MicroscopePixelSizeCacheItem.Minima(t => t.LensInformation.LensCode).Single().LensInformation;
        return true;
    }
}
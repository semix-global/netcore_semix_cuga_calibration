using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Pattern;
using MoreLinq;
using Net.Utilities.DataAnnotations;
using Net.Utilities.Models.Enums.Maths;
using Net.Utilities.Models.Geometries;
using System.Collections.ObjectModel;

namespace Core.Models.Models.Microscope.Focus;

public sealed partial class MicroscopeFocusCache : CalibrationCacheBase
{
    private double _threshold = 50;

    [ObservableProperty]
    private MicroscopeMagnificationInfo _microscopeMagnificationInfo = new();

    [ObservableProperty]
    private double _verifyResultError;

    [ObservableProperty]
    private double _verifyResultQuality;

    [Comparison(1d, NumberComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "Threshold: ")]
    public double Threshold
    {
        get => _threshold;
        set => SetProperty(ref _threshold, value, validate: true);
    }

    [ObservableProperty]
    private int _parfocalThreshold;

    [ObservableProperty]
    private ObservableCollection<MicroscopeFocusCacheItem> _microscopeFocusCacheItems = [];

    public void SetFindFocusPosition(Point position)
    {
        var info = MicroscopeFocusCacheItems.SingleOrDefault(item => item.MagnificationInfo == MicroscopeMagnificationInfo) ?? throw new ArgumentNullException(nameof(SetFindFocusPosition));
        info.FindFocusPosition = position;
    }

    public MicroscopeFocusCacheItem GetSelectedCacheItem()
    {
        return MicroscopeFocusCacheItems.SingleOrDefault(item => item.MagnificationInfo == MicroscopeMagnificationInfo) ?? throw new ArgumentNullException(nameof(GetSelectedCacheItem));
    }


    public bool InitializeCacheList(List<MicroscopeMagnificationInfo> microscopeMagnificationInfoList)
    {
        if (microscopeMagnificationInfoList.Count == 0) return false;
        var isInitialized = MicroscopeFocusCacheItems.Count == microscopeMagnificationInfoList.Count
                            && MicroscopeFocusCacheItems.Select((item, index) => (index, item))
                                .All(t => t.item.MagnificationInfo == microscopeMagnificationInfoList[t.index]);
        if (isInitialized) return true;
        MicroscopeFocusCacheItems = new ObservableCollection<MicroscopeFocusCacheItem>(
            microscopeMagnificationInfoList.Select(t => new MicroscopeFocusCacheItem() { MagnificationInfo = t.Clone() }));
        MicroscopeMagnificationInfo = MicroscopeFocusCacheItems.Minima(t => t.MagnificationInfo.MagnificationCode).Single().MagnificationInfo;
        return true;
    }

    public (bool IsSuccess, string ErrorMessage) CalibrationVerify(MicroscopeMagnificationInfo microscopeMagnificationInfo)
    {
        ClearErrors();
        var info = MicroscopeFocusCacheItems.FirstOrDefault(item => item.MagnificationInfo.Equals(microscopeMagnificationInfo)) ?? throw new ArgumentNullException(nameof(CalibrationVerify));
        info.CacheItemVerify();

        return HasErrors ? (false, string.Join(Environment.NewLine, GetErrors())) : (true, string.Empty);
    }
}
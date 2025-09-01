using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Pattern;
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
    private MicroscopeLensInformation _microscopeLensInformation =  MicroscopeLensInformation.Default;

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
        var info = MicroscopeFocusCacheItems.SingleOrDefault(item => item.LensInformation == MicroscopeLensInformation) ?? throw new ArgumentNullException(nameof(SetFindFocusPosition));
        info.FindFocusPosition = position;
    }

    public MicroscopeFocusCacheItem GetSelectedCacheItem()
    {
        return MicroscopeFocusCacheItems.SingleOrDefault(item => item.LensInformation == MicroscopeLensInformation) ?? throw new ArgumentNullException(nameof(GetSelectedCacheItem));
    }

    public bool InitializeCacheList(List<MicroscopeLensInformation> microscopeLensInformationList)
    {
        if (microscopeLensInformationList.Count == 0) return false;
        var isInitialized = MicroscopeFocusCacheItems.Count == microscopeLensInformationList.Count
                            && MicroscopeFocusCacheItems.Select((item, index) => (index, item))
                                .All(t => t.item.LensInformation == microscopeLensInformationList[t.index]);
        if (isInitialized) return true;
        MicroscopeFocusCacheItems = new ObservableCollection<MicroscopeFocusCacheItem>(
            microscopeLensInformationList.Select(t => new MicroscopeFocusCacheItem { LensInformation = t.Clone() }));
        MicroscopeLensInformation = MicroscopeFocusCacheItems.Minima(t => t.LensInformation.LensCode).Single().LensInformation;
        return true;
    }

    public (bool IsSuccess, string ErrorMessage) CalibrationVerify(MicroscopeLensInformation microscopeLensInformation)
    {
        ClearErrors();
        var info = MicroscopeFocusCacheItems.FirstOrDefault(item => item.LensInformation.Equals(microscopeLensInformation)) ?? throw new ArgumentNullException(nameof(CalibrationVerify));
        info.CacheItemVerify();

        return HasErrors ? (false, string.Join(Environment.NewLine, GetErrors())) : (true, string.Empty);
    }
}
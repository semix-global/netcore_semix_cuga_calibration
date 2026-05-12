using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Mapper.Interfaces;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.DataAnnotations;
using Net.Utilities.Models.Enums.Maths;
using Net.Utilities.Models.Geometries;
using System.Collections.Concurrent;

namespace Core.Models.Models.Microscope.Focus;

public sealed partial class MicroscopeFocusCache : CalibrationCacheBase<MicroscopeFocusCache>
{
    private double _threshold = 50;

    [ObservableProperty]
    private MicroscopeLensInformation _microscopeLensInformation = MicroscopeLensInformation.Default;

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
    private ConcurrentDictionary<string, MicroscopeFocusCacheItem> _microscopeFocusCacheItemDic = [];

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    public MicroscopeFocusCacheItem CurrentCalibrationCacheItem =>
        MicroscopeFocusCacheItemDic.GetOrAdd(MicroscopeLensInformation.LensName, new MicroscopeFocusCacheItem { LensInformation = MicroscopeLensInformation.Clone() });

    public void SetFindFocusPosition(Point position)
    {
        CurrentCalibrationCacheItem.FindFocusPosition = position;
    }

    public (bool IsSuccess, string ErrorMessage) CalibrationVerify()
    {
        ClearErrors();
        CurrentCalibrationCacheItem.CacheItemVerify();

        return HasErrors ? (false, string.Join(Environment.NewLine, GetErrors())) : (true, string.Empty);
    }

    public override MicroscopeFocusCache Clone() => new()
    {
        MicroscopeLensInformation = MicroscopeLensInformation.Clone(),
        VerifyResultError = VerifyResultError,
        VerifyResultQuality = VerifyResultQuality,
        Threshold = Threshold,
        ParfocalThreshold = ParfocalThreshold,
        MicroscopeFocusCacheItemDic = new([.. MicroscopeFocusCacheItemDic]),
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration
    };
}
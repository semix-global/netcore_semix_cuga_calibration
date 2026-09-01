using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Recipe.Wafer;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.DataAnnotations;
using Net.Utilities.Models.Enums.Maths;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Models.Serializations;
using System.Collections.Concurrent;

namespace Core.Models.Models.Microscope.Focus;

public sealed partial class MicroscopeFocusCache : CalibrationCacheBase<MicroscopeFocusCache>
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Item))]
    public partial MicroscopeLensInformation MicroscopeLensInformation { get; set; } = MicroscopeLensInformation.Default;

    [Comparison(1d, NumberComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "Threshold: ")]
    public double Threshold
    {
        get;
        set => SetProperty(ref field, value, validate: true);
    } = 50;

    [ObservableProperty]
    public partial double VerifyResultError { get; set; }

    [ObservableProperty]
    public partial double VerifyResultQuality { get; set; }

    [ObservableProperty]
    public partial int ParfocalThreshold { get; set; }

    [Newtonsoft.Json.JsonConverter(typeof(DictionaryConverter<MicroscopeLensInformation, MicroscopeFocusCacheItem>))]
    public ConcurrentDictionary<MicroscopeLensInformation, MicroscopeFocusCacheItem> Items { get; init; } = [];

    [Newtonsoft.Json.JsonIgnore]
    public MicroscopeFocusCacheItem Item => Items.GetOrAdd(MicroscopeLensInformation, _ => new MicroscopeFocusCacheItem());

    public (bool IsSuccess, string ErrorMessage) CalibrationVerify()
    {
        ClearErrors();
        ValidateProperty(Threshold, nameof(Threshold));
        var validateResult = Item.CacheItemVerify();

        return HasErrors ? validateResult : (true, string.Empty);
    }

    public override MicroscopeFocusCache Clone() => new()
    {
        MicroscopeLensInformation = MicroscopeLensInformation.Clone(),
        VerifyResultError = VerifyResultError,
        VerifyResultQuality = VerifyResultQuality,
        Threshold = Threshold,
        ParfocalThreshold = ParfocalThreshold,
        Items = new ConcurrentDictionary<MicroscopeLensInformation, MicroscopeFocusCacheItem>(Items.Select(t => new KeyValuePair<MicroscopeLensInformation, MicroscopeFocusCacheItem>(t.Key.Clone(), t.Value.Clone()))),
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration
    };
}

public sealed partial class MicroscopeFocusCacheItem : CalibrationCacheBase<MicroscopeFocusCacheItem>
{
    [ObservableProperty]
    public partial WaferMaskTypeEnum WaferMaskTypeEnum { get; set; } = WaferMaskTypeEnum.Undefined;

    [ObservableProperty]
    public partial Point FindFocusPosition { get; set; }

    [Comparison(3000d, NumberComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "FindFocusMin: ")]
    public double StartECS
    {
        get;
        set => SetProperty(ref field, value, validate: true);
    } = 3000;

    [Comparison(10000d, NumberComparisonTypeEnum.LessThanOrEqual, ErrorMessage = "FindFocusMax: ")]
    public double StopECS
    {
        get;
        set => SetProperty(ref field, value, validate: true);
    } = 10000;

    [ComparisonRange(1d, 100d, NumberComparisonRangeTypeEnum.ClosedInterval, ErrorMessage = "FindFocusInterval: ")]
    public double StepECS
    {
        get;
        set => SetProperty(ref field, value, validate: true);
    } = 5;

    [ObservableProperty]
    [Comparison(1d, NumberComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "SetVoltageAfErrorThreshold: ")]
    public partial double SetVoltageAfErrorThreshold { get; set; }


    public (bool IsSuccess, string ErrorMessage) CacheItemVerify()
    {
        ClearErrors();
        ValidateProperty(StartECS, nameof(StartECS));
        ValidateProperty(StopECS, nameof(StopECS));
        ValidateProperty(StepECS, nameof(StepECS));
        ValidateProperty(SetVoltageAfErrorThreshold, nameof(SetVoltageAfErrorThreshold));

        return HasErrors ? (false, string.Join(Environment.NewLine, GetErrors())) : (true, string.Empty);
    }

    public override MicroscopeFocusCacheItem Clone() => new()
    {
        WaferMaskTypeEnum = WaferMaskTypeEnum,
        FindFocusPosition = FindFocusPosition,
        StartECS = StartECS,
        StopECS = StopECS,
        StepECS = StepECS,
        SetVoltageAfErrorThreshold = SetVoltageAfErrorThreshold,
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration
    };
}
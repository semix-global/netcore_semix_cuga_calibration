using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Recipe.Wafer;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.DataAnnotations;
using Net.Utilities.Models.Enums.Maths;
using Net.Utilities.Models.Geometries;
using System.Collections.Concurrent;

namespace Core.Models.Models.Microscope.Focus;

public sealed partial class MicroscopeFocusCache : CalibrationCacheBase<MicroscopeFocusCache>
{
    [ObservableProperty]
    public partial MicroscopeLensInformation MicroscopeLensInformation { get; set; } = MicroscopeLensInformation.Default;

    [ObservableProperty]
    public partial double VerifyResultError { get; set; }

    [ObservableProperty]
    public partial double VerifyResultQuality { get; set; }

    [Comparison(1d, NumberComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "Threshold: ")]
    public double Threshold
    {
        get;
        set => SetProperty(ref field, value, validate: true);
    } = 50;

    [ObservableProperty]
    public partial int ParfocalThreshold { get; set; }

    [ObservableProperty]
    public partial ConcurrentDictionary<string, MicroscopeFocusCacheItem> MicroscopeFocusCacheItemDic { get; set; } = [];

    [Newtonsoft.Json.JsonIgnore]
    public MicroscopeFocusCacheItem CurrentCalibrationCacheItem => MicroscopeFocusCacheItemDic.GetOrAdd(MicroscopeLensInformation.LensName, new MicroscopeFocusCacheItem { LensInformation = MicroscopeLensInformation.Clone() });

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
        MicroscopeFocusCacheItemDic = new ConcurrentDictionary<string, MicroscopeFocusCacheItem>(MicroscopeFocusCacheItemDic.Select(t => new KeyValuePair<string, MicroscopeFocusCacheItem>(t.Key, t.Value.Clone()))),
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration
    };
}

public sealed partial class MicroscopeFocusCacheItem : CalibrationCacheBase<MicroscopeFocusCacheItem>
{
    [ObservableProperty]
    public partial MicroscopeLensInformation LensInformation { get; set; } = MicroscopeLensInformation.Default;

    [ObservableProperty]
    public partial WaferMaskTypeEnum WaferMaskTypeEnum { get; set; } = WaferMaskTypeEnum.Undefined;

    [ObservableProperty]
    public partial Point FindFocusPosition { get; set; }

    [Comparison(3000d, NumberComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "FindFocusMin: ")]
    public double FindFocusMin
    {
        get;
        set => SetProperty(ref field, value, validate: true);
    } = 3000;

    [Comparison(10000d, NumberComparisonTypeEnum.LessThanOrEqual, ErrorMessage = "FindFocusMax: ")]
    public double FindFocusMax
    {
        get;
        set => SetProperty(ref field, value, validate: true);
    } = 10000;

    [ComparisonRange(1d, 100d, NumberComparisonRangeTypeEnum.ClosedInterval, ErrorMessage = "FindFocusInterval: ")]
    public double FindFocusInterval
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
        ValidateProperty(FindFocusMin, nameof(FindFocusMin));
        ValidateProperty(FindFocusMax, nameof(FindFocusMax));
        ValidateProperty(FindFocusInterval, nameof(FindFocusInterval));
        ValidateProperty(SetVoltageAfErrorThreshold, nameof(SetVoltageAfErrorThreshold));

        return HasErrors ? (false, string.Join(Environment.NewLine, GetErrors())) : (true, string.Empty);
    }

    public override MicroscopeFocusCacheItem Clone() => new()
    {
        LensInformation = LensInformation.Clone(),
        WaferMaskTypeEnum = WaferMaskTypeEnum,
        FindFocusPosition = FindFocusPosition,
        FindFocusMin = FindFocusMin,
        FindFocusMax = FindFocusMax,
        FindFocusInterval = FindFocusInterval,
        SetVoltageAfErrorThreshold = SetVoltageAfErrorThreshold,
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration
    };

    #region Eqauls

    /// <summary>
    /// 确定指定的对象是否等于当前对象，仅比较 LensInformation
    /// </summary>
    /// <param name="obj">要与当前对象进行比较的对象</param>
    /// <returns>如果指定的对象的 LensInformation 与当前对象的相等，则为 true；否则为 false</returns>
    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj is MicroscopeFocusCacheItem item && Equals(item);
    }

    /// <summary>
    /// 确定指定的 MicroscopeFocusCacheItem 是否等于当前 MicroscopeFocusCacheItem，仅比较 LensInformation
    /// </summary>
    /// <param name="other">要与当前对象进行比较的 MicroscopeFocusCacheItem</param>
    /// <returns>如果指定的 MicroscopeFocusCacheItem 的 LensInformation 与当前对象的相等，则为 true；否则为 false</returns>
    private bool Equals(MicroscopeFocusCacheItem? other)
    {
        if (other is null) return false;
        return ReferenceEquals(this, other) ||
               // 仅比较 _lensInformation
               LensInformation.Equals(other.LensInformation);
    }

    /// <summary>
    /// 返回基于 LensInformation 的哈希码
    /// </summary>
    /// <returns>当前对象的哈希码</returns>
    public override int GetHashCode()
    {
        return LensInformation?.GetHashCode() ?? 0;
    }

    /// <summary>
    /// 确定两个 MicroscopeFocusCacheItem 实例的 LensInformation 是否相等
    /// </summary>
    public static bool operator ==(MicroscopeFocusCacheItem? left, MicroscopeFocusCacheItem? right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    /// <summary>
    /// 确定两个 MicroscopeFocusCacheItem 实例的 LensInformation 是否不相等
    /// </summary>
    public static bool operator !=(MicroscopeFocusCacheItem? left, MicroscopeFocusCacheItem? right)
    {
        return !(left == right);
    }

    #endregion Eqauls
}
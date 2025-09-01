using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Recipe.Wafer;
using Core.Models.Models.Common.Pattern;
using Local.NoSQL.DB.Providers.Bases;
using Net.Utilities.DataAnnotations;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Enums.Maths;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Microscope.Focus;

public sealed partial class MicroscopeFocusCacheItem : ObservableCacheBase, ICloneable<MicroscopeFocusCacheItem>
{
    [ObservableProperty]
    private MicroscopeLensInformation _lensInformation =  MicroscopeLensInformation.Default;

    [ObservableProperty]
    private WaferMaskTypeEnum _waferMaskTypeEnum = WaferMaskTypeEnum.Undefined;

    [ObservableProperty]
    private Point _findFocusPosition;

    private double _findFocusMin = 3000;

    private double _findFocusMax = 10000;

    private double _findFocusInterval = 5;

    [Comparison(3000d, NumberComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "FindFocusMin: ")]
    public double FindFocusMin
    {
        get => _findFocusMin;
        set => SetProperty(ref _findFocusMin, value, validate: true);
    }

    [Comparison(10000d, NumberComparisonTypeEnum.LessThanOrEqual, ErrorMessage = "FindFocusMax: ")]
    public double FindFocusMax
    {
        get => _findFocusMax;
        set => SetProperty(ref _findFocusMax, value, validate: true);
    }

    [ComparisonRange(1d, 100d, NumberComparisonRangeTypeEnum.ClosedInterval, ErrorMessage = "FindFocusInterval: ")]
    public double FindFocusInterval
    {
        get => _findFocusInterval;
        set => SetProperty(ref _findFocusInterval, value, validate: true);
    }

    [ObservableProperty]
    [Comparison(1d, NumberComparisonTypeEnum.GreaterThanOrEqual, ErrorMessage = "SetVoltageAfErrorThreshold: ")]
    private double _setVoltageAfErrorThreshold;

    public (bool IsSuccess, string ErrorMessage) CacheItemVerify()
    {
        ClearErrors();
        ValidateProperty(FindFocusMin, nameof(FindFocusMin));
        ValidateProperty(FindFocusMax, nameof(FindFocusMax));
        ValidateProperty(FindFocusInterval, nameof(FindFocusInterval));
        ValidateProperty(SetVoltageAfErrorThreshold, nameof(SetVoltageAfErrorThreshold));

        return HasErrors ? (false, string.Join(Environment.NewLine, GetErrors())) : (true, string.Empty);
    }

    public MicroscopeFocusCacheItem Clone() => new()
    {
        LensInformation = LensInformation.Clone(),
        WaferMaskTypeEnum = WaferMaskTypeEnum,
        FindFocusPosition = FindFocusPosition,
        FindFocusMin = FindFocusMin,
        FindFocusMax = FindFocusMax,
        FindFocusInterval = FindFocusInterval,
        SetVoltageAfErrorThreshold = SetVoltageAfErrorThreshold
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
using Net.Utilities.Enums.Maths;
using System.ComponentModel.DataAnnotations;

namespace Net.Utilities.Attributes.DataAnnotations;

#if NET
public sealed class ComparisonAttribute<T> : ValidationAttribute where T : IComparable
{
    private readonly T _minValue;
    private readonly T? _maxValue;
    private readonly bool _isRangeValidation;

    public ComparisonTypeEnum ComparisonType { get; }


    // 构造函数 1：单值比较
    public ComparisonAttribute(T targetValue, ComparisonTypeEnum comparisonTypeEnum)
    {
        _minValue = targetValue;
        _maxValue = default;
        _isRangeValidation = false;
        ComparisonType = comparisonTypeEnum;
    }

    /// <summary>
    /// 构造函数 2：范围校验 [min, max]
    /// </summary>
    public ComparisonAttribute(T minValue, T maxValue, ComparisonTypeEnum comparisonTypeEnum)
    {
        _minValue = minValue;
        _maxValue = maxValue;
        _isRangeValidation = true;
        ComparisonType = comparisonTypeEnum;
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var memberNames = validationContext.MemberName is null ? null : new[] { validationContext.MemberName };
        var formatErrorMessage = FormatErrorMessage(validationContext.DisplayName);

        if (value is not T comparableValue
            || comparableValue.GetType() != _minValue.GetType()
            || (_maxValue is not null && comparableValue.GetType() != _maxValue.GetType()))
        {
            return new ValidationResult($"{formatErrorMessage}: Invalid data type. Only comparable types are allowed.", memberNames);
        }

        var comparisonMinResult = comparableValue.CompareTo(_minValue);
        var comparisonMaxResult = comparableValue.CompareTo(_maxValue);

        if (_isRangeValidation)
            return ComparisonType switch
            {
                ComparisonTypeEnum.ClosedInterval => (comparisonMinResult >= 0 && comparisonMaxResult <= 0) ? ValidationResult.Success : new ValidationResult($"{formatErrorMessage}: The value must be greater than or equal {_minValue} and less than or equal {_maxValue}.", memberNames),
                ComparisonTypeEnum.OpenInterval => (comparisonMinResult > 0 && comparisonMaxResult < 0) ? ValidationResult.Success : new ValidationResult($"{formatErrorMessage}: The value must be greater than {_minValue} and less than {_maxValue}.", memberNames),
                ComparisonTypeEnum.LeftOpenAndRightClosedInterval => (comparisonMinResult > 0 && comparisonMaxResult <= 0) ? ValidationResult.Success : new ValidationResult($"{formatErrorMessage}: The value must be greater than {_minValue} and less than or equal {_maxValue}.", memberNames),
                ComparisonTypeEnum.LeftClosedAndRightOpenInterval => (comparisonMinResult >= 0 && comparisonMaxResult < 0) ? ValidationResult.Success : new ValidationResult($"{formatErrorMessage}: The value must be greater than or equal {_minValue} and less than  {_maxValue}.", memberNames),
                _ => new ValidationResult($"{formatErrorMessage}: Invalid comparison type.", memberNames)
            };

        // 单值比较逻辑
        return ComparisonType switch
        {
            ComparisonTypeEnum.GreaterThan => comparisonMinResult > 0 ? ValidationResult.Success : new ValidationResult($"{formatErrorMessage}: The value must be greater than {_minValue}.", memberNames),
            ComparisonTypeEnum.GreaterThanOrEqual => comparisonMinResult >= 0 ? ValidationResult.Success : new ValidationResult($"{formatErrorMessage}: The value must be greater than or equal to {_minValue}.", memberNames),
            ComparisonTypeEnum.LessThan => comparisonMinResult < 0 ? ValidationResult.Success : new ValidationResult($"{formatErrorMessage}: The value must be less than {_minValue}.", memberNames),
            ComparisonTypeEnum.LessThanOrEqual => comparisonMinResult <= 0 ? ValidationResult.Success : new ValidationResult($"{formatErrorMessage}: The value must be less than or equal to {_minValue}.", memberNames),
            _ => new ValidationResult($"{formatErrorMessage}: Invalid comparison type.", memberNames)
        };
    }
}
#endif

public sealed class ComparisonAttribute : ValidationAttribute
{
    private readonly object _minValue;
    private readonly object? _maxValue;
    private readonly bool _isRangeValidation;

    public ComparisonTypeEnum ComparisonType { get; }


    // 构造函数 1：单值比较
    public ComparisonAttribute(object targetValue, ComparisonTypeEnum comparisonTypeEnum)
    {
        _minValue = targetValue;
        _maxValue = null;
        _isRangeValidation = false;
        ComparisonType = comparisonTypeEnum;
    }

    // 构造函数 2：范围校验 [min, max]
    public ComparisonAttribute(object minValue, object maxValue, ComparisonTypeEnum comparisonTypeEnum)
    {
        _minValue = minValue;
        _maxValue = maxValue;
        _isRangeValidation = true;
        ComparisonType = comparisonTypeEnum;
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var memberNames = validationContext.MemberName is null ? null : new[] { validationContext.MemberName };
        var formatErrorMessage = FormatErrorMessage(validationContext.DisplayName);

        if (value is not IComparable comparableValue
            || comparableValue.GetType() != _minValue.GetType()
            || (_maxValue is not null && comparableValue.GetType() != _maxValue.GetType()))
        {
            return new ValidationResult($"{formatErrorMessage}: Invalid data type. Only comparable types are allowed.", memberNames);
        }

        var comparisonMinResult = comparableValue.CompareTo(_minValue);
        var comparisonMaxResult = comparableValue.CompareTo(_maxValue);

        if (_isRangeValidation)
            return ComparisonType switch
            {
                ComparisonTypeEnum.ClosedInterval => (comparisonMinResult >= 0 && comparisonMaxResult <= 0) ? ValidationResult.Success : new ValidationResult($"{formatErrorMessage}: The value must be greater than or equal {_minValue} and less than or equal {_maxValue}.", memberNames),
                ComparisonTypeEnum.OpenInterval => (comparisonMinResult > 0 && comparisonMaxResult < 0) ? ValidationResult.Success : new ValidationResult($"{formatErrorMessage}: The value must be greater than {_minValue} and less than {_maxValue}.", memberNames),
                ComparisonTypeEnum.LeftOpenAndRightClosedInterval => (comparisonMinResult > 0 && comparisonMaxResult <= 0) ? ValidationResult.Success : new ValidationResult($"{formatErrorMessage}: The value must be greater than {_minValue} and less than or equal {_maxValue}.", memberNames),
                ComparisonTypeEnum.LeftClosedAndRightOpenInterval => (comparisonMinResult >= 0 && comparisonMaxResult < 0) ? ValidationResult.Success : new ValidationResult($"{formatErrorMessage}: The value must be greater than or equal {_minValue} and less than  {_maxValue}.", memberNames),
                _ => new ValidationResult($"{formatErrorMessage}: Invalid comparison type.", memberNames)
            };

        // 单值比较逻辑
        return ComparisonType switch
        {
            ComparisonTypeEnum.GreaterThan => comparisonMinResult > 0 ? ValidationResult.Success : new ValidationResult($"{formatErrorMessage}: The value must be greater than {_minValue}.", memberNames),
            ComparisonTypeEnum.GreaterThanOrEqual => comparisonMinResult >= 0 ? ValidationResult.Success : new ValidationResult($"{formatErrorMessage}: The value must be greater than or equal to {_minValue}.", memberNames),
            ComparisonTypeEnum.LessThan => comparisonMinResult < 0 ? ValidationResult.Success : new ValidationResult($"{formatErrorMessage}: The value must be less than {_minValue}.", memberNames),
            ComparisonTypeEnum.LessThanOrEqual => comparisonMinResult <= 0 ? ValidationResult.Success : new ValidationResult($"{formatErrorMessage}: The value must be less than or equal to {_minValue}.", memberNames),
            _ => new ValidationResult($"{formatErrorMessage}: Invalid comparison type.", memberNames)
        };
    }
}
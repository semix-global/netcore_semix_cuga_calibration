using CommunityToolkit.Diagnostics;
using Net.Utilities.WPF.Converters.MultiValue;
using System.Globalization;
using System.Windows;

namespace Core.Utilities.WPF.Converters.MultiValueConverters;

public sealed class MultiValueBooleanAnyConverter : AbstractSingletonMultiConverterBase<MultiValueBooleanAnyConverter>
{
    public static readonly DependencyProperty IsNegationProperty = DependencyProperty.Register(
        nameof(IsNegation),
        typeof(bool),
        typeof(MultiValueBooleanAnyConverter),
        new PropertyMetadata(false));

    /// <summary>
    /// 可选：支持输入的bool反转逻辑
    /// </summary>
    public bool IsNegation
    {
        get => (bool)GetValue(IsNegationProperty);
        set => SetValue(IsNegationProperty, value);
    }

    public override object Convert(object?[]? values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values is null) return ThrowHelper.ThrowNotSupportedException<object>(nameof(values));

        var booleans = values.Cast<bool>().ToArray();
        var result = booleans.Any(t => t);

        return IsNegation ? !result : result;
    }

    public override object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture) => ThrowHelper.ThrowNotSupportedException<object[]>(nameof(value));
}
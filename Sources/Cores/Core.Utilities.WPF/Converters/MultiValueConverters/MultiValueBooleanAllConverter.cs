using Net.Utilities.WPF.Converters.MultiValueConverters;
using System.Globalization;
using System.Windows;
using CommunityToolkit.Diagnostics;
using ValueConverters;

namespace Core.Utilities.WPF.Converters.MultiValueConverters;

public sealed class MultiValueBooleanAllConverter : AbstractSingletonMultiConverterBase<MultiValueBooleanAllConverter>
{
    public static readonly DependencyProperty IsNegationProperty = DependencyProperty.Register(
        nameof(IsNegation),
        typeof(bool),
        typeof(BoolToVisibilityConverter),
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
        var result = booleans.All(t => t);

        return IsNegation ? !result : result;
    }

    public override object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture) => ThrowHelper.ThrowNotSupportedException<object[]>(nameof(value));
}
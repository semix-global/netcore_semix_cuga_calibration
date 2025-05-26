using Net.Utilities.Helper.Enum;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Net.Utilities.WPF.Converters;

public sealed class StringToNullableBoolConverter : IValueConverter
{
    public required string EnumType { get; set; }
    public required string IgnoreObject { get; set; }
    public required string TrueObject { get; set; }
    public required string FalseObject { get; set; }

    private Type? _enumType;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var enumStringValue = IgnoreObject;
        if (value is bool boolValue)
            enumStringValue = boolValue ? TrueObject : FalseObject;

        return _enumType is not null ? Enum.Parse(_enumType, enumStringValue) : DependencyProperty.UnsetValue;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null)
        {
            return null;
        }

        _enumType = value.GetType();
        if (_enumType.Name != EnumType) return DependencyProperty.UnsetValue;
        var descriptionStringstringValue = EnumHelper.ToDescriptionString(value);
        return descriptionStringstringValue.Equals(TrueObject) ? true
            : descriptionStringstringValue.Equals(FalseObject) ? false
            : null;
    }
}
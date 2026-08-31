using Net.Utilities.Helpers.Helpers.Structs;
using System.Globalization;
using System.Windows;

namespace Core.Utilities.WPF.Converters;

using Net.Utilities.WPF.Converters.SingleValue;

public sealed class StringToNullableBoolConverter : AbstractSingletonConverterBase<StringToNullableBoolConverter>
{
    public string EnumType { get; set; } = "EnableStatusEnum";
    public string IgnoreObject { get; set; } = "Ignore";
    public string TrueObject { get; set; } = "Enable";
    public string FalseObject { get; set; } = "Disable";

    private Type? _enumType;

    public override object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var enumStringValue = IgnoreObject;
        if (value is bool boolValue)
            enumStringValue = boolValue ? TrueObject : FalseObject;

        return _enumType is not null ? Enum.Parse(_enumType, enumStringValue) : DependencyProperty.UnsetValue;
    }

    public override object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null)
        {
            return DependencyProperty.UnsetValue;
        }

        _enumType = value.GetType();
        if (_enumType.Name != EnumType) return DependencyProperty.UnsetValue;
        var descriptionStringValue = EnumHelper.ToDescriptionString(value);

        return descriptionStringValue.Equals(TrueObject) ? true
            : descriptionStringValue.Equals(FalseObject) ? false
            : DependencyProperty.UnsetValue;
    }
}
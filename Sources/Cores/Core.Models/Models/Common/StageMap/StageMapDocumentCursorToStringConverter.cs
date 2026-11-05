using System.Globalization;
using System.Windows;
using CommunityToolkit.Diagnostics;
using Net.Utilities.WPF.Converters.MultiValue;
using Point = Net.Utilities.Models.Geometries.Point;

namespace Core.Models.Models.Common.StageMap;

public sealed class StageMapDocumentCursorToStringConverter : AbstractSingletonMultiConverterBase<StageMapDocumentCursorToStringConverter>
{
    private const string DefaultString = "0, 0 | (0.0, 0.0)";

    public override object Convert(object?[]? values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values?[0] == DependencyProperty.UnsetValue) return DefaultString;
        if (values is not [Point point, StageMapDocument document]) return ThrowHelper.ThrowNotSupportedException<object>(nameof(values));

        var result = $"0, 0 | {point.ToString(document.Settings.NumberFormat)}";

        var selectionPickDistance = document.View.ScreenToWorldDistance(document.Settings.SelectionPickDistance);

        using var scope = document.View.Sync.EnterScope();
        foreach (var die in document.DieModel)
        {
            if (die.Contains(point, selectionPickDistance)) return $"{die.Index} | {point.ToString(document.Settings.NumberFormat)}";
        }

        return result;
    }

    public override object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture) => ThrowHelper.ThrowNotSupportedException<object[]>(nameof(value));
}
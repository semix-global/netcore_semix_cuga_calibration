using CommunityToolkit.Diagnostics;
using CugaCalibration.ViewModels.Common.Windows.Tools.Stage;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using Point = Net.Utilities.Models.Geometries.Point;

namespace CugaCalibration.Views.Common.Windows.Tools.Stage;

public partial class StageMapWindow
{
    public StageMapWindow()
    {
        InitializeComponent();
    }
}

internal sealed class StageMapDocumentCursorToStringConverter : MarkupExtension, IMultiValueConverter
{
    private const string DefaultString = "0, 0 | (0.0, 0.0)";

    public object Convert(object[]? values, Type targetType, object parameter, CultureInfo culture)
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

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => ThrowHelper.ThrowNotSupportedException<object[]>(nameof(value));

    public override object ProvideValue(IServiceProvider serviceProvider) => this;
}
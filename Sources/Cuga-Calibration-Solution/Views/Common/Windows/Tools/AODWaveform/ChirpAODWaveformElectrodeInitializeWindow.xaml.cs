using CommunityToolkit.Diagnostics;
using CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace CugaCalibration.Views.Common.Windows.Tools.AODWaveform;

public partial class ChirpAODWaveformElectrodeInitializeWindow
{
    public ChirpAODWaveformElectrodeInitializeWindow()
    {
        InitializeComponent();
    }
}

public sealed class ChirpAODWaveformElectrodeInitializeConvert : MarkupExtension, IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not IEnumerable<ChirpAODWaveformElectrodeInitializeItem> items) return ThrowHelper.ThrowNotSupportedException<object>();

        var item = items.FirstOrDefault();

        return item is null ? DependencyProperty.UnsetValue : $"{item.Frequency}MHz";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => ThrowHelper.ThrowNotSupportedException<object>();

    public override object ProvideValue(IServiceProvider serviceProvider) => this;
}
using CommunityToolkit.Diagnostics;
using CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace CugaCalibration.Views.Common.Windows.Tools.AODWaveform;

public partial class V0ChirpAODWaveformElectrodeOffsetWindow
{
    public V0ChirpAODWaveformElectrodeOffsetWindow()
    {
        InitializeComponent();
    }
}

public sealed class V0ChirpAODWaveformElectrodeOffsetConvert : MarkupExtension, IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not IEnumerable<V0ChirpAODWaveformElectrodeOffsetItem> items) return ThrowHelper.ThrowNotSupportedException<object>();

        var item = items.FirstOrDefault();

        return item is null ? DependencyProperty.UnsetValue : $"{item.Frequency}MHz";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => ThrowHelper.ThrowNotSupportedException<object>();

    public override object ProvideValue(IServiceProvider serviceProvider) => this;
}
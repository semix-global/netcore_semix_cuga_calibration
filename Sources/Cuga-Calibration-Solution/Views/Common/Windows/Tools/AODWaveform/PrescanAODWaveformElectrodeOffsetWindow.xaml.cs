using CommunityToolkit.Diagnostics;
using CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace CugaCalibration.Views.Common.Windows.Tools.AODWaveform;

public partial class PrescanAODWaveformElectrodeOffsetWindow
{
    public PrescanAODWaveformElectrodeOffsetWindow()
    {
        InitializeComponent();
    }
}

public sealed class PrescanAODWaveformElectrodeOffsetConvert : MarkupExtension, IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not PrescanAODWaveformElectrodeOffsetItem[] items || parameter is not string format) return ThrowHelper.ThrowNotSupportedException<object>();

        return string.Join(", ", items.Select(t => $"{t.Frequency.ToString(format)}: {t.Amplitude.ToString(format)}"));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => ThrowHelper.ThrowNotSupportedException<object>();

    public override object ProvideValue(IServiceProvider serviceProvider) => this;
}
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;
using CommunityToolkit.Diagnostics;
using CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

namespace CugaCalibration.Views.Common.Windows.Tools.AODWaveform;

public partial class ChirpAODWaveformTrainingWindow
{
    public ChirpAODWaveformTrainingWindow()
    {
        InitializeComponent();
    }
}

public sealed class ChirpAODWaveformTrainingConvert : MarkupExtension, IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        return values is [ChirpAODWaveformTrainingSlope chirpAODWaveformTrainingSlope, bool isSilent]
            ? (chirpAODWaveformTrainingSlope, isSilent)
            : ThrowHelper.ThrowNotSupportedException<object>();
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => ThrowHelper.ThrowNotSupportedException<object[]>();

    public override object ProvideValue(IServiceProvider serviceProvider) => this;
}
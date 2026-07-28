using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using System.ComponentModel;

#if NETFRAMEWORK
using Core.Models.Extensions;
#endif

namespace Core.Models.Models.Common.AODWaveform.Generates;

public sealed partial class GenerateAODWaveformElectrodeConfiguration :
    ObservableObject,
    IAdaptTo<AODWaveformGenerator1.AODWaveformOffsetConfiguration>,
    ICloneable<GenerateAODWaveformElectrodeConfiguration>
{
    [ObservableProperty]
    public partial OpticsAODElectrodeEnum OpticsAODElectrodeEnum { get; set; }

    [ObservableProperty]
    public partial double OffsetFrequency { get; set; }

    [ObservableProperty]
    public partial double OffsetFrequencyPeriodCoefficient { get; set; }

    [ObservableProperty]
    public partial double Amplitude { get; set; } = 1d;

    [ObservableProperty]
    public partial bool IsGenerateAODWaveformZero { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<GenerateAODWaveformUniformityConfiguration> UniformityConfigurations { get; set; } = [];

    partial void OnUniformityConfigurationsChanged(IReadOnlyList<GenerateAODWaveformUniformityConfiguration> oldValue, IReadOnlyList<GenerateAODWaveformUniformityConfiguration> newValue)
    {
        foreach (var item in oldValue) item.PropertyChanged -= ItemOnPropertyChanged;

        foreach (var item in newValue)
        {
            item.PropertyChanged -= ItemOnPropertyChanged;
            item.PropertyChanged += ItemOnPropertyChanged;
        }

        OnPropertyChanged(nameof(UniformityConfigurations));

        return;

        void ItemOnPropertyChanged(object? sender, PropertyChangedEventArgs e) => OnPropertyChanged(nameof(UniformityConfigurations));
    }

    public GenerateAODWaveformElectrodeConfiguration WithAmplitude(double amplitude)
    {
        Amplitude = amplitude;

        return this;
    }

    public GenerateAODWaveformElectrodeConfiguration WithUniformityConfigurations(IReadOnlyList<GenerateAODWaveformUniformityConfiguration> uniformityConfigurations)
    {
        UniformityConfigurations = uniformityConfigurations;

        return this;
    }

    public AODWaveformGenerator1.AODWaveformOffsetConfiguration AdaptTo() => new(
#if NETFRAMEWORK
        OpticsAODElectrodeEnum.ToCgAwgElectrodeEnum().ToString(),
#else
        OpticsAODElectrodeEnum.ToString(),
#endif
        OffsetFrequency,
        OffsetFrequencyPeriodCoefficient,
        Amplitude,
        IsGenerateAODWaveformZero)
    {
        UniformityConfigurations = [.. UniformityConfigurations.Select(t => t.AdaptTo())]
    };

    public GenerateAODWaveformElectrodeConfiguration Clone() => new()
    {
        OpticsAODElectrodeEnum = OpticsAODElectrodeEnum,
        OffsetFrequency = OffsetFrequency,
        OffsetFrequencyPeriodCoefficient = OffsetFrequencyPeriodCoefficient,
        Amplitude = Amplitude,
        IsGenerateAODWaveformZero = IsGenerateAODWaveformZero,
        UniformityConfigurations = [.. UniformityConfigurations.Select(t => t.Clone())]
    };

    public object ToHtmlAnonymous() => new
    {
        OpticsAODElectrodeEnum,
        OffsetFrequency,
        OffsetFrequencyPeriodCoefficient,
        Amplitude,
        IsGenerateAODWaveformZero,
        UniformityConfigurations = new HtmlPlot2DLinesChart([(string.Empty, [.. UniformityConfigurations.Select(t => new Point(t.Frequency, t.Coefficient))])], string.Empty)
    };

    public object ToFlatnessHtmlAnonymous() => new
    {
        OpticsAODElectrodeEnum,
        OffsetFrequency,
        OffsetFrequencyPeriodCoefficient,
        Amplitude,
        IsGenerateAODWaveformZero
    };
}
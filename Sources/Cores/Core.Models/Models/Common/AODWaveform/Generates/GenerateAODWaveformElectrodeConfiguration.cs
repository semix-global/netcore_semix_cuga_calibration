using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Local.NoSQL.DB.Providers.Bases;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Nlog.Entities.HtmlElements;

namespace Core.Models.Models.Common.AODWaveform.Generates;

public sealed partial class GenerateAODWaveformElectrodeConfiguration :
    ObservableCacheBase,
    IAdaptTo<AODWaveformGenerator.AODWaveformOffsetConfiguration>,
    IAdaptIn<AbstractAODWaveformProfile, GenerateAODWaveformElectrodeConfiguration>,
    ICloneable<GenerateAODWaveformElectrodeConfiguration>
{
    [ObservableProperty]
    private OpticsAODElectrodeEnum _opticsAODElectrodeEnum;

    [ObservableProperty]
    private double _offsetFrequency;

    [ObservableProperty]
    private double _offsetFrequencyPeriodCoefficient;

    [ObservableProperty]
    private double _amplitude = 1d;

    [ObservableProperty]
    private bool _isGenerateAODWaveformZero;

    [ObservableProperty]
    private IReadOnlyList<GenerateAODWaveformUniformityConfiguration> _uniformityConfigurations = [];

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

    public AODWaveformGenerator.AODWaveformOffsetConfiguration AdaptTo() => new(
        OpticsAODElectrodeEnum.ToString(),
        OffsetFrequency,
        OffsetFrequencyPeriodCoefficient,
        Amplitude,
        IsGenerateAODWaveformZero)
    {
        UniformityConfigurations = [..UniformityConfigurations.Select(t => t.AdaptTo())]
    };

    [Obsolete]
    public GenerateAODWaveformElectrodeConfiguration AdaptIn(AbstractAODWaveformProfile obj)
    {
        OpticsAODElectrodeEnum = obj.OpticsAODElectrodeEnum;
        OffsetFrequency = obj.OffsetFrequency;
        OffsetFrequencyPeriodCoefficient = obj.OffsetFrequencyPeriodCoefficient;

        return this;
    }

    public GenerateAODWaveformElectrodeConfiguration Clone() => new()
    {
        OpticsAODElectrodeEnum = OpticsAODElectrodeEnum,
        OffsetFrequency = OffsetFrequency,
        OffsetFrequencyPeriodCoefficient = OffsetFrequencyPeriodCoefficient,
        UniformityConfigurations = [..UniformityConfigurations.Select(t => t.Clone())]
    };

    public object ToHtmlAnonymous() => new
    {
        OpticsAODElectrodeEnum,
        OffsetFrequency,
        OffsetFrequencyPeriodCoefficient,
        Amplitude,
        IsGenerateAODWaveformZero,
        UniformityConfigurations = new HtmlTable([.. UniformityConfigurations.Select(t => t.ToHtmlAnonymous())])
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
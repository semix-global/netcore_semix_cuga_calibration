using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Common.AODWaveform.Generates;

public sealed partial class GenerateAODWaveformUniformityConfiguration :
    ObservableObject,
    IAdaptTo<AODWaveformGenerator1.AODWaveformUniformityConfiguration>,
    ICloneable<GenerateAODWaveformUniformityConfiguration>
{
    [ObservableProperty]
    private double _frequency;

    [ObservableProperty]
    private double _coefficient;

    public AODWaveformGenerator1.AODWaveformUniformityConfiguration AdaptTo() => new(Frequency, Coefficient);

    public GenerateAODWaveformUniformityConfiguration Clone() => new()
    {
        Frequency = Frequency,
        Coefficient = Coefficient
    };
}
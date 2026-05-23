using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Common.AODWaveform.Generates;

public sealed partial class GenerateAODWaveformUniformityConfiguration :
    ObservableObject,
    IAdaptTo<AODWaveformGenerator.AODWaveformUniformityConfiguration>,
    ICloneable<GenerateAODWaveformUniformityConfiguration>
{
    [ObservableProperty]
    public partial double Frequency { get; set; }

    [ObservableProperty]
    public partial double Coefficient { get; set; }

    public AODWaveformGenerator.AODWaveformUniformityConfiguration AdaptTo() => new(Frequency, Coefficient);

    public GenerateAODWaveformUniformityConfiguration Clone() => new()
    {
        Frequency = Frequency,
        Coefficient = Coefficient
    };
}
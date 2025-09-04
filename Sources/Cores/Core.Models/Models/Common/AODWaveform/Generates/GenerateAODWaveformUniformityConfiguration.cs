using CommunityToolkit.Mvvm.ComponentModel;
using Core.Utilities;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Common.AODWaveform.Generates;

public sealed partial class GenerateAODWaveformUniformityConfiguration : ObservableObject, IAdaptTo<AODWaveformGenerator.AODWaveformUniformityConfiguration>
{
    [ObservableProperty]
    private double _frequency;

    [ObservableProperty]
    private double _coefficient;

    public AODWaveformGenerator.AODWaveformUniformityConfiguration AdaptTo() => new(Frequency, Coefficient);
}
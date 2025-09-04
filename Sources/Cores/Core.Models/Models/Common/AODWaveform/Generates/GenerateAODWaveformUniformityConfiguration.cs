using CommunityToolkit.Mvvm.ComponentModel;
using Core.Utilities;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Common.AODWaveform.Generates;

public sealed partial class GenerateAODWaveformUniformityConfiguration : ObservableObject, IAdaptTo<AODWaveformGenerator.AODWaveformUniformityConfiguration>
{
    [ObservableProperty]
    private double _centerFrequency;

    [ObservableProperty]
    private double _coefficient;

    public AODWaveformGenerator.AODWaveformUniformityConfiguration AdaptTo() => new(CenterFrequency, Coefficient);
}
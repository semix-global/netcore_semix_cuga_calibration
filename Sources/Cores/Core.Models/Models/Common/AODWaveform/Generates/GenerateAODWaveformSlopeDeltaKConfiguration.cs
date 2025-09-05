using CommunityToolkit.Mvvm.ComponentModel;
using Core.Utilities;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Common.AODWaveform.Generates;

public sealed partial class GenerateAODWaveformSlopeDeltaKConfiguration : ObservableObject, IAdaptTo<AODWaveformGenerator.AODWaveformSlopeDeltaKConfiguration>
{
    [ObservableProperty]
    private double _deltaK;

    public AODWaveformGenerator.AODWaveformSlopeDeltaKConfiguration AdaptTo() => new(DeltaK);
}
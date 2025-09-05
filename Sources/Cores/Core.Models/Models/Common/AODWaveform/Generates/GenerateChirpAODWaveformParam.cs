using CommunityToolkit.Mvvm.ComponentModel;
using Core.Utilities;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Common.AODWaveform.Generates;

public sealed partial class GenerateChirpAODWaveformParam : AbstractGenerateAODWaveformParam, IAdaptTo<AODWaveformGenerator.ChirpAODWaveformParam>
{
    [ObservableProperty]
    private double _soundPackageLength = 3.2;

    [ObservableProperty]
    private double _soundSpeed = 5.742;

    public AODWaveformGenerator.ChirpAODWaveformParam AdaptTo() => AdaptIn(new AODWaveformGenerator.ChirpAODWaveformParam(SoundPackageLength, SoundSpeed));
}
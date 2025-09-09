using CommunityToolkit.Mvvm.ComponentModel;
using Core.Utilities;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Common.AODWaveform.Generates;

public sealed partial class GeneratePrescanAODWaveformParam : AbstractGenerateAODWaveformParam, IAdaptTo<AODWaveformGenerator.PrescanAODWaveformParam>
{
    [ObservableProperty]
    private double _flatnessTime = 4300;

    public AODWaveformGenerator.PrescanAODWaveformParam AdaptTo() => CopyPropertiesTo(new AODWaveformGenerator.PrescanAODWaveformParam(FlatnessTime));
}
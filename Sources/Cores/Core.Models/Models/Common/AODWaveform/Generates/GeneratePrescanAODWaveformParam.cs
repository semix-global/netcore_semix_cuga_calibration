using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Common.AODWaveform.Generates;

public sealed partial class GeneratePrescanAODWaveformParam : GenerateAODWaveformParamBase
{
    [ObservableProperty]
    private double _flatnessTime = 4300;
}
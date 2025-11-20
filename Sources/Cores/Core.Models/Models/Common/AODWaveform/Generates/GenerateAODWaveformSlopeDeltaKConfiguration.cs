using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Common.AODWaveform.Generates;

public sealed partial class GenerateAODWaveformSlopeDeltaKConfiguration :
    ObservableObject,
    IAdaptTo<AODWaveformGenerator.AODWaveformSlopeDeltaKConfiguration>,
    ICloneable<GenerateAODWaveformSlopeDeltaKConfiguration>
{
    [ObservableProperty]
    private double _deltaK;

    public AODWaveformGenerator.AODWaveformSlopeDeltaKConfiguration AdaptTo() => new(DeltaK);

    public GenerateAODWaveformSlopeDeltaKConfiguration Clone() => new()
    {
        DeltaK = DeltaK
    };

    public object ToHtmlAnonymous() => new
    {
        DeltaK
    };
}
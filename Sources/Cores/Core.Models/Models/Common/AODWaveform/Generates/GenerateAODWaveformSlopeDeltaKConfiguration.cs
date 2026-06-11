using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Common.AODWaveform.Generates;

public sealed partial class GenerateAODWaveformSlopeDeltaKConfiguration :
    ObservableObject,
    IAdaptTo<AODWaveformGenerator1.AODWaveformSlopeDeltaKConfiguration>,
    ICloneable<GenerateAODWaveformSlopeDeltaKConfiguration>
{
    [ObservableProperty]
    public partial double DeltaKRate { get; set; }

    public AODWaveformGenerator1.AODWaveformSlopeDeltaKConfiguration AdaptTo() => new(DeltaKRate);

    public GenerateAODWaveformSlopeDeltaKConfiguration Clone() => new()
    {
        DeltaKRate = DeltaKRate
    };

    public object ToHtmlAnonymous() => new
    {
        DeltaKRate
    };
}
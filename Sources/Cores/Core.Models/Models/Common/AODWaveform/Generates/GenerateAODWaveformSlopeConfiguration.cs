using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Common.AODWaveform.Generates;

public sealed partial class GenerateAODWaveformSlopeConfiguration :
    ObservableObject,
    IAdaptTo<AODWaveformGenerator1.AODWaveformSlopeConfiguration>,
    ICloneable<GenerateAODWaveformSlopeConfiguration>
{
    [ObservableProperty]
    public partial double DeltaKRate { get; set; }

    [ObservableProperty]
    public partial double Coefficient { get; set; } = 1d;

    public AODWaveformGenerator1.AODWaveformSlopeConfiguration AdaptTo() => new(DeltaKRate, Coefficient);

    public GenerateAODWaveformSlopeConfiguration Clone() => new()
    {
        DeltaKRate = DeltaKRate,
        Coefficient = Coefficient
    };

    public object ToHtmlAnonymous() => new
    {
        DeltaKRate,
        Coefficient
    };
}
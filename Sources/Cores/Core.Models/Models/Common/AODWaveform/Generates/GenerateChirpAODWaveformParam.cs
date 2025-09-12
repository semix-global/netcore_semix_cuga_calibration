using CommunityToolkit.Mvvm.ComponentModel;
using Core.Utilities;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Nlog.Entities.HtmlElements;

namespace Core.Models.Models.Common.AODWaveform.Generates;

public sealed partial class GenerateChirpAODWaveformParam : AbstractGenerateAODWaveformParam, IAdaptTo<AODWaveformGenerator.ChirpAODWaveformParam>
{
    [ObservableProperty]
    private double _soundPackageLength = 3.2;

    [ObservableProperty]
    private double _soundSpeed = 5.742;

    public AODWaveformGenerator.ChirpAODWaveformParam AdaptTo() => CopyPropertiesTo(new AODWaveformGenerator.ChirpAODWaveformParam(SoundPackageLength, SoundSpeed));

    public override object ToHtmlAnonymous() => new
    {
        SoundPackageLength,
        SoundSpeed,
        Base = new HtmlBullet(base.ToHtmlAnonymous())
    };
}
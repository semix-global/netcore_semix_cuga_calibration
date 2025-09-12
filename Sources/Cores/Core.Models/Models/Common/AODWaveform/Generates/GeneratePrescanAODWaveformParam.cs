using CommunityToolkit.Mvvm.ComponentModel;
using Core.Utilities;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Nlog.Entities.HtmlElements;

namespace Core.Models.Models.Common.AODWaveform.Generates;

public sealed partial class GeneratePrescanAODWaveformParam : AbstractGenerateAODWaveformParam, IAdaptTo<AODWaveformGenerator.PrescanAODWaveformParam>
{
    [ObservableProperty]
    private double _flatnessTime = 4300;

    public AODWaveformGenerator.PrescanAODWaveformParam AdaptTo() => CopyPropertiesTo(new AODWaveformGenerator.PrescanAODWaveformParam(FlatnessTime));

    public override object ToHtmlAnonymous() => new
    {
        FlatnessTime,
        OpticsMagTypeEnum,
        Base = new HtmlBullet(base.ToHtmlAnonymous())
    };
}
using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Nlog.Entities.HtmlElements;

namespace Core.Models.Models.Common.AODWaveform.Generates;

public sealed partial class GeneratePrescanAODWaveformParam :
    AbstractGenerateAODWaveformParam,
    IAdaptTo<AODWaveformGenerator1.PrescanAODWaveformParam>,
    ICloneable<GeneratePrescanAODWaveformParam>
{
    [ObservableProperty]
    private double _flatnessTime = 4300;

    public AODWaveformGenerator1.PrescanAODWaveformParam AdaptTo() => new(FlatnessTime)
    {
        BandWidth = BandWidth,
        CenterFrequency = CenterFrequency,
        FunctionMonotonicTypeEnum = FunctionMonotonicTypeEnum,
        SampleRate = SampleRate,
        DirectoryPath = DirectoryPath,
        FileNameSuffix = $"{ProductivityInformation.OpticsIlluminationModeEnum}_{ProductivityInformation.AdaptTo().Mag}",
        ZeroSampleCount = ZeroSampleCount,
        EndpointSampleCount = EndpointSampleCount,
        OffsetConfigurations = [.. ElectrodeConfigurations.Select(t => t.AdaptTo())],
        P2CompensationCoefficient = P2CompensationCoefficient,
        P3CompensationCoefficient = P3CompensationCoefficient,
        P4CompensationCoefficient = P4CompensationCoefficient,
        P5CompensationCoefficient = P5CompensationCoefficient,
        P6CompensationCoefficient = P6CompensationCoefficient,
        P7CompensationCoefficient = P7CompensationCoefficient,
        P8CompensationCoefficient = P8CompensationCoefficient
    };

    public GeneratePrescanAODWaveformParam Clone()
    {
        var param = (GeneratePrescanAODWaveformParam)new GeneratePrescanAODWaveformParam().AdaptIn(this);

        param.FlatnessTime = FlatnessTime;

        return param;
    }

    public override object ToFlatnessHtmlAnonymous() => new
    {
        FlatnessTime,
        Base = new HtmlQuote(base.ToFlatnessHtmlAnonymous())
    };

    public override object ToHtmlAnonymous() => new
    {
        FlatnessTime,
        Base = new HtmlQuote(base.ToHtmlAnonymous())
    };
}
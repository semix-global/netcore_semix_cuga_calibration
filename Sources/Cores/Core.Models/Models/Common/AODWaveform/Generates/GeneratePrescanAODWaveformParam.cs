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
        GenerateRetryTimes = GenerateRetryTimes,
        OffsetConfigurations = [.. ElectrodeConfigurations.Select(t => t.AdaptTo())],
        C2CompensationCoefficient = C2CompensationCoefficient,
        C3CompensationCoefficient = C3CompensationCoefficient,
        C4CompensationCoefficient = C4CompensationCoefficient,
        C5CompensationCoefficient = C5CompensationCoefficient,
        C6CompensationCoefficient = C6CompensationCoefficient,
        C7CompensationCoefficient = C7CompensationCoefficient,
        C8CompensationCoefficient = C8CompensationCoefficient
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
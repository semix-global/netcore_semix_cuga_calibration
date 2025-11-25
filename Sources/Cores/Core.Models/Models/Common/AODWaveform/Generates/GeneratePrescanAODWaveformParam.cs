using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Nlog.Entities.HtmlElements;

namespace Core.Models.Models.Common.AODWaveform.Generates;

public sealed partial class GeneratePrescanAODWaveformParam :
    AbstractGenerateAODWaveformParam,
    IAdaptTo<AODWaveformGenerator.PrescanAODWaveformParam>,
    ICloneable<GeneratePrescanAODWaveformParam>
{
    [ObservableProperty]
    private double _flatnessTime = 4300;

    public AODWaveformGenerator.PrescanAODWaveformParam AdaptTo() => new(FlatnessTime)
    {
        BandWidth = BandWidth,
        CenterFrequency = CenterFrequency,
        FunctionMonotonicTypeEnum = FunctionMonotonicTypeEnum,
        SampleRate = SampleRate,
        DirectoryPath = DirectoryPath,
        FileNameSuffix = ProductivityInformation.AdaptTo().Mag.ToString(),
        ZeroSampleCount = ZeroSampleCount,
        EndpointSampleCount = EndpointSampleCount,
        GenerateRetryTimes = GenerateRetryTimes,
        OffsetConfigurations = [.. ElectrodeConfigurations.Select(t => t.AdaptTo())],
        SlopeDeltaKConfigurations = [.. SlopeDeltaKConfigurations.Select(t => t.AdaptTo())],
        SincCoefficient = SincCoefficient,
        AstigmatismCompensationCoefficient = AstigmatismCompensationCoefficient,
        SphericalAberrationCompensationCoefficient = SphericalAberrationCompensationCoefficient,
        SecondaryAstigmatismCompensationCoefficient = SecondaryAstigmatismCompensationCoefficient,
        ComaCompensationCoefficient = ComaCompensationCoefficient,
        TrefoilCompensationCoefficient = TrefoilCompensationCoefficient,
        QuadrafoilCompensationCoefficient = QuadrafoilCompensationCoefficient,
        AlphaOrder = AlphaOrder,
        AlphaOrderCoefficient = AlphaOrderCoefficient
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
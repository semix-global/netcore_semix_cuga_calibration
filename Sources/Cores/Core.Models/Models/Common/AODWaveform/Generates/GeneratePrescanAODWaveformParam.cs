using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Extensions;
using Core.Utilities;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Nlog.Entities.HtmlElements;

namespace Core.Models.Models.Common.AODWaveform.Generates;

public sealed partial class GeneratePrescanAODWaveformParam : AbstractGenerateAODWaveformParam, IAdaptTo<AODWaveformGenerator.PrescanAODWaveformParam>
{
    [ObservableProperty]
    private double _flatnessTime = 4300;

    public AODWaveformGenerator.PrescanAODWaveformParam AdaptTo() => new (FlatnessTime)
    {
        BandWidth = BandWidth,
        CenterFrequency = CenterFrequency,
        FunctionMonotonicTypeEnum = FunctionMonotonicTypeEnum,
        SampleRate = SampleRate,
        Amplitude = Amplitude,
        DirectoryPath = DirectoryPath,
        FileNameSuffix = OpticsMagTypeEnum.ToCgMagTypeEnum().ToString(),
        ZeroSampleCount = ZeroSampleCount,
        EndpointSampleCount = EndpointSampleCount,
        GenerateRetryTimes = GenerateRetryTimes,
        OffsetConfigurations = [.. ElectrodeConfigurations.Select(t => t.AdaptTo())],
        UniformityConfigurations = [.. UniformityConfigurations.Select(t => t.AdaptTo())],
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

    public override object ToHtmlAnonymous() => new
    {
        FlatnessTime,
        OpticsMagTypeEnum,
        Base = new HtmlBullet(base.ToHtmlAnonymous())
    };
}
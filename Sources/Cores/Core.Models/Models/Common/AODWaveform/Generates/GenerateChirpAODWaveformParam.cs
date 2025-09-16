using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Extensions;
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

    public AODWaveformGenerator.ChirpAODWaveformParam AdaptTo() => new(SoundPackageLength, SoundSpeed)
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
        SoundPackageLength,
        SoundSpeed,
        Base = new HtmlBullet(base.ToHtmlAnonymous())
    };
}
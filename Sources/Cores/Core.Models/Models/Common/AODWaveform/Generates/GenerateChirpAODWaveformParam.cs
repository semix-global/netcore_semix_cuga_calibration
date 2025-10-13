using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Extensions;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Nlog.Entities.HtmlElements;

namespace Core.Models.Models.Common.AODWaveform.Generates;

public sealed partial class GenerateChirpAODWaveformParam :
    AbstractGenerateAODWaveformParam,
    IAdaptTo<AODWaveformGenerator.ChirpAODWaveformParam>,
    ICloneable<GenerateChirpAODWaveformParam>
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SpectralDensity))]
    private double _soundPacketLength = 3.2;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SpectralDensity))]
    private double _soundSpeed = 5.742;

    public double SpectralDensity => SoundPacketLength != 0 ? BandWidth / SoundPacketLength : 0;

    public AODWaveformGenerator.ChirpAODWaveformParam AdaptTo() => new(SoundPacketLength, SoundSpeed)
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

    public GenerateChirpAODWaveformParam Clone()
    {
        var param = (GenerateChirpAODWaveformParam)new GenerateChirpAODWaveformParam().AdaptIn(this);

        param.SoundPacketLength = SoundPacketLength;
        param.SoundSpeed = SoundSpeed;

        return param;
    }

    public override object ToHtmlAnonymous() => new
    {
        SoundPacketLength,
        SoundSpeed,
        Base = new HtmlQuote(base.ToHtmlAnonymous())
    };
}
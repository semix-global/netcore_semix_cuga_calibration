using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Common.DarkField;

public sealed partial class GenerateChirpAodWaveParamDto : GenerateAodWaveParamBase, ICloneable<GenerateChirpAodWaveParamDto>
{
    [NotifyPropertyChangedFor(nameof(FrequencyChangeRate))]
    [ObservableProperty]
    private double _soundPackageLength = 3.2;

    public double FrequencyChangeRate => BandWidth / SoundPackageLength;

    public GenerateChirpAodWaveParamDto Clone() => new()
    {
        SoundPackageLength = SoundPackageLength,
        IsHeaderAndFooter = IsHeaderAndFooter,
        BandWidth = BandWidth,
        CenterFrequency = CenterFrequency,
        HeaderFrequency = HeaderFrequency,
        FooterFrequency = FooterFrequency,
        FunctionMonotonicTypeEnum = FunctionMonotonicTypeEnum,
        SampleRate = SampleRate,
        Amplitude = Amplitude,
        AodWaveDirectory = AodWaveDirectory,
        ZeroSampleCount = ZeroSampleCount,
        EndpointSampleCount = EndpointSampleCount,
        OffsetFrequency = OffsetFrequency,
        OffsetFrequencyPeriodMultiple = OffsetFrequencyPeriodMultiple,
        SincCoefficient = SincCoefficient,
        AstigmatismCompensationCoefficient = AstigmatismCompensationCoefficient,
        SphericalAberrationCompensationCoefficient = SphericalAberrationCompensationCoefficient,
        SecondaryAstigmatismCompensationCoefficient = SecondaryAstigmatismCompensationCoefficient,
        ComaCompensationCoefficient = ComaCompensationCoefficient,
        TrefoilCompensationCoefficient = TrefoilCompensationCoefficient,
        QuadrafoilCompensationCoefficient = QuadrafoilCompensationCoefficient,
        AlphaOrder = AlphaOrder,
        AlphaOrderCoefficient = AlphaOrderCoefficient,
        GenerateRetryTimes = GenerateRetryTimes,
        FrequencyAmplitudesFilePath = FrequencyAmplitudesFilePath
    };
}
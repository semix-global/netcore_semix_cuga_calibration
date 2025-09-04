using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Extensions;
using Core.Utilities;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Common.AODWaveform.Generates;

public sealed partial class GenerateChirpAODWaveformParam : AbstractGenerateAODWaveformParam, IAdaptTo<AODWaveformGenerator.ChirpAODWaveformParam>
{
    [ObservableProperty]
    private double _soundPackageLength = 3.2;

    [ObservableProperty]
    private double _soundSpeed = 5.742;

    public AODWaveformGenerator.ChirpAODWaveformParam AdaptTo() => new(
        BandWidth,
        CenterFrequency,
        SoundPackageLength,
        SoundSpeed,
        FunctionMonotonicTypeEnum,
        SampleRate,
        Amplitude,
        DirectoryPath,
        ZeroSampleCount,
        EndpointSampleCount,
        GenerateRetryTimes,
        [.. ElectrodeConfigurations.Select(t => t.AdaptTo())],
        [..UniformityConfigurations.Select(t => t.AdaptTo())],
        SincCoefficient,
        AstigmatismCompensationCoefficient,
        SphericalAberrationCompensationCoefficient,
        SecondaryAstigmatismCompensationCoefficient,
        ComaCompensationCoefficient,
        TrefoilCompensationCoefficient,
        QuadrafoilCompensationCoefficient,
        AlphaOrder,
        AlphaOrderCoefficient)
    {
        FileNameSuffix = OpticsMagTypeEnum.ToCgMagTypeEnum().ToString()
    };
}
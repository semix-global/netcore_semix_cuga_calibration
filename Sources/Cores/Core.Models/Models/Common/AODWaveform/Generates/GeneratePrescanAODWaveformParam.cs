using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Extensions;
using Core.Utilities;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Common.AODWaveform.Generates;

public sealed partial class GeneratePrescanAODWaveformParam : AbstractGenerateAODWaveformParam, IAdaptTo<AODWaveformGenerator.PrescanAODWaveformParam>
{
    [ObservableProperty]
    private double _flatnessTime = 4300;

    public AODWaveformGenerator.PrescanAODWaveformParam AdaptTo() => new(
        BandWidth,
        CenterFrequency,
        FlatnessTime,
        FunctionMonotonicTypeEnum,
        SampleRate,
        Amplitude,
        DirectoryPath,
        [.. ElectrodeConfigurations.Select(t => t.AdaptTo())],
        ZeroSampleCount,
        EndpointSampleCount,
        SincCoefficient,
        AstigmatismCompensationCoefficient,
        SphericalAberrationCompensationCoefficient,
        SecondaryAstigmatismCompensationCoefficient,
        ComaCompensationCoefficient,
        TrefoilCompensationCoefficient,
        QuadrafoilCompensationCoefficient,
        AlphaOrder,
        AlphaOrderCoefficient,
        FrequencyAmplitudes,
        GenerateRetryTimes)
    {
        FileNameSuffix = OpticsMagTypeEnum.ToCgMagTypeEnum().ToString()
    };
}
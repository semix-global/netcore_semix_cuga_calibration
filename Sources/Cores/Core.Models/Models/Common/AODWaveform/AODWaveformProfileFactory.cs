using CommunityToolkit.Diagnostics;
using Core.Models.Enums.Optics;
using Core.Utilities;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.Mapper.Interfaces;
using System.IO;

namespace Core.Models.Models.Common.AODWaveform;

public static class AODWaveformProfileFactory
{
    public static PrescanAODWaveformProfile CreatePrescan(OpticsAODElectrodeEnum opticsAODElectrodeEnum, string filePath, double coefficient = 1d, int? customZeroSampleCount = null)
    {
        Guard.IsTrue(File.Exists(filePath), $"File not found: {filePath}");

        var aodWaveProfile = new PrescanAODWaveformProfile
        {
            OpticsAODElectrodeEnum = opticsAODElectrodeEnum,
            FilePath = filePath
        };

        aodWaveProfile.ApplyCoefficient(coefficient);

        if (customZeroSampleCount is not null) aodWaveProfile.ZeroSampleCount = customZeroSampleCount.Value;

        return aodWaveProfile;
    }

    public static IReadOnlyList<PrescanAODWaveformProfile> CreatePrescanList(IReadOnlyList<PrescanAODWaveformResult> prescanAODWaveformResultList) => prescanAODWaveformResultList
        .Cast<IAdaptTo<PrescanAODWaveformProfile>>()
        .Select(t => t.AdaptTo())
        .ToList();

    public static ChirpAODWaveformProfile CreateChirp(OpticsAODElectrodeEnum opticsAODElectrodeEnum, string filePath, int? customZeroSampleCount = null)
    {
        Guard.IsTrue(File.Exists(filePath), $"File not found: {filePath}");

        var chirpAODWaveformProfile = new ChirpAODWaveformProfile
        {
            OpticsAODElectrodeEnum = opticsAODElectrodeEnum,
            FilePath = filePath
        };

        if (customZeroSampleCount is not null) chirpAODWaveformProfile.ZeroSampleCount = customZeroSampleCount.Value;

        return chirpAODWaveformProfile;
    }

    public static IReadOnlyList<ChirpAODWaveformProfile> CreateChirpList(IReadOnlyList<ChirpAODWaveformResult> chirpAODWaveformResultList) => chirpAODWaveformResultList
        .Cast<IAdaptTo<ChirpAODWaveformProfile>>()
        .Select(t => t.AdaptTo())
        .ToList();

    public static IReadOnlyList<PrescanAODWaveformProfile> CreatePrescanList(string prescanAODWaveformResultFilePath)
    {
        Guard.IsEqualTo(Path.GetExtension(prescanAODWaveformResultFilePath), AODWaveformGenerator.PrescanAODWaveformFileExtension, "File Extension is not valid.");

        var prescanAODWaveformResult = FileHelper.DeserializeOperate<AODWaveformGenerator.PrescanAODWaveformResult>(prescanAODWaveformResultFilePath);
        Guard.IsNotNull(prescanAODWaveformResult, "File Extension is not valid.");

        return CreatePrescanList(prescanAODWaveformResult);
    }

    public static IReadOnlyList<ChirpAODWaveformProfile> CreateChirpList(string chirpAODWaveformResultFilePath)
    {
        Guard.IsEqualTo(Path.GetExtension(chirpAODWaveformResultFilePath), AODWaveformGenerator.ChirpAODWaveformFileExtension, "File Extension is not valid.");

        var chirpAODWaveformResult = FileHelper.DeserializeOperate<AODWaveformGenerator.ChirpAODWaveformResult>(chirpAODWaveformResultFilePath);
        Guard.IsNotNull(chirpAODWaveformResult, "File Extension is not valid.");

        return CreateChirpList(chirpAODWaveformResult);
    }

    public static IReadOnlyList<PrescanAODWaveformProfile> CreatePrescanList(AODWaveformGenerator.PrescanAODWaveformResult prescanAODWaveformResult, double coefficient = 1d)
    {
        var result = new List<PrescanAODWaveformProfile>();
        foreach (var item in prescanAODWaveformResult.Items)
        {
            Guard.IsTrue(Enum.TryParse<OpticsAODElectrodeEnum>(item.OffsetConfiguration.DirectoryName, out var opticsAODElectrodeEnum), "Directory Name is not valid.");

            var prescanAODWaveformProfile = CreatePrescan(opticsAODElectrodeEnum, item.FilePath, coefficient);
            prescanAODWaveformProfile.Signals = [.. item.Signals];
            prescanAODWaveformProfile.FFTSignals = [.. item.FFTSignals];
            prescanAODWaveformProfile.FrequencyCoefficients = [.. item.FrequencyCoefficients];
            prescanAODWaveformProfile.FlatnessLinearFrequencySignals = [.. item.FlatnessLinearFrequencySignals];
            prescanAODWaveformProfile.FlatnessTotalFrequencySignals = [.. item.FlatnessTotalFrequencySignals];
            prescanAODWaveformProfile.FlatnessAstigmatismCompensationSignals = [.. item.FlatnessAstigmatismCompensationSignals];
            prescanAODWaveformProfile.FlatnessSphericalAberrationCompensationSignals = [.. item.FlatnessSphericalAberrationCompensationSignals];
            prescanAODWaveformProfile.FlatnessSecondaryAstigmatismCompensationSignals = [.. item.FlatnessSecondaryAstigmatismCompensationSignals];
            prescanAODWaveformProfile.FlatnessComaCompensationSignals = [.. item.FlatnessComaCompensationSignals];
            prescanAODWaveformProfile.FlatnessTrefoilCompensationSignals = [.. item.FlatnessTrefoilCompensationSignals];
            prescanAODWaveformProfile.FlatnessQuadrafoilCompensationSignals = [.. item.FlatnessQuadrafoilCompensationSignals];
            prescanAODWaveformProfile.FlatnessAlphaOrderCompensationSignals = [.. item.FlatnessAlphaOrderCompensationSignals];

            result.Add(prescanAODWaveformProfile);
        }

        return result;
    }

    public static IReadOnlyList<ChirpAODWaveformProfile> CreateChirpList(AODWaveformGenerator.ChirpAODWaveformResult chirpAODWaveformResult)
    {
        var result = new List<ChirpAODWaveformProfile>();
        foreach (var item in chirpAODWaveformResult.Items)
        {
            Guard.IsTrue(Enum.TryParse<OpticsAODElectrodeEnum>(item.OffsetConfiguration.DirectoryName, out var opticsAODElectrodeEnum), "Directory Name is not valid.");

            var chirpAODWaveformProfile = CreateChirp(opticsAODElectrodeEnum, item.FilePath);
            chirpAODWaveformProfile.Signals = [.. item.Signals];
            chirpAODWaveformProfile.FFTSignals = [.. item.FFTSignals];
            chirpAODWaveformProfile.FrequencyCoefficients = [.. item.FrequencyCoefficients];
            chirpAODWaveformProfile.FlatnessLinearFrequencySignals = [.. item.FlatnessLinearFrequencySignals];
            chirpAODWaveformProfile.FlatnessTotalFrequencySignals = [.. item.FlatnessTotalFrequencySignals];
            chirpAODWaveformProfile.FlatnessAstigmatismCompensationSignals = [.. item.FlatnessAstigmatismCompensationSignals];
            chirpAODWaveformProfile.FlatnessSphericalAberrationCompensationSignals = [.. item.FlatnessSphericalAberrationCompensationSignals];
            chirpAODWaveformProfile.FlatnessSecondaryAstigmatismCompensationSignals = [.. item.FlatnessSecondaryAstigmatismCompensationSignals];
            chirpAODWaveformProfile.FlatnessComaCompensationSignals = [.. item.FlatnessComaCompensationSignals];
            chirpAODWaveformProfile.FlatnessTrefoilCompensationSignals = [.. item.FlatnessTrefoilCompensationSignals];
            chirpAODWaveformProfile.FlatnessQuadrafoilCompensationSignals = [.. item.FlatnessQuadrafoilCompensationSignals];
            chirpAODWaveformProfile.FlatnessAlphaOrderCompensationSignals = [.. item.FlatnessAlphaOrderCompensationSignals];

            result.Add(chirpAODWaveformProfile);
        }

        return result;
    }
}
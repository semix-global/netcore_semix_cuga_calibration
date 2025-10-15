using CommunityToolkit.Diagnostics;
using Core.Models.Enums.Optics;
using Net.Utilities.Algorithms.Modules;
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
            if (item.Signals.Count > 0) prescanAODWaveformProfile.Signals = [.. item.Signals];
            if (item.FFTSignals.Count > 0) prescanAODWaveformProfile.FFTSignals = [.. item.FFTSignals];
            if (item.FrequencyCoefficients.Count > 0) prescanAODWaveformProfile.FrequencyCoefficients = [.. item.FrequencyCoefficients];
            if (item.FlatnessLinearFrequencySignals.Count > 0) prescanAODWaveformProfile.FlatnessLinearFrequencySignals = [.. item.FlatnessLinearFrequencySignals];
            if (item.FlatnessTotalFrequencySignals.Count > 0) prescanAODWaveformProfile.FlatnessTotalFrequencySignals = [.. item.FlatnessTotalFrequencySignals];
            if (item.FlatnessAstigmatismCompensationSignals.Count > 0) prescanAODWaveformProfile.FlatnessAstigmatismCompensationSignals = [.. item.FlatnessAstigmatismCompensationSignals];
            if (item.FlatnessSphericalAberrationCompensationSignals.Count > 0) prescanAODWaveformProfile.FlatnessSphericalAberrationCompensationSignals = [.. item.FlatnessSphericalAberrationCompensationSignals];
            if (item.FlatnessSecondaryAstigmatismCompensationSignals.Count > 0) prescanAODWaveformProfile.FlatnessSecondaryAstigmatismCompensationSignals = [.. item.FlatnessSecondaryAstigmatismCompensationSignals];
            if (item.FlatnessComaCompensationSignals.Count > 0) prescanAODWaveformProfile.FlatnessComaCompensationSignals = [.. item.FlatnessComaCompensationSignals];
            if (item.FlatnessTrefoilCompensationSignals.Count > 0) prescanAODWaveformProfile.FlatnessTrefoilCompensationSignals = [.. item.FlatnessTrefoilCompensationSignals];
            if (item.FlatnessQuadrafoilCompensationSignals.Count > 0) prescanAODWaveformProfile.FlatnessQuadrafoilCompensationSignals = [.. item.FlatnessQuadrafoilCompensationSignals];
            if (item.FlatnessAlphaOrderCompensationSignals.Count > 0) prescanAODWaveformProfile.FlatnessAlphaOrderCompensationSignals = [.. item.FlatnessAlphaOrderCompensationSignals];

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
            if (item.Signals.Count > 0) chirpAODWaveformProfile.Signals = [.. item.Signals];
            if (item.FFTSignals.Count > 0) chirpAODWaveformProfile.FFTSignals = [.. item.FFTSignals];
            if (item.FrequencyCoefficients.Count > 0) chirpAODWaveformProfile.FrequencyCoefficients = [.. item.FrequencyCoefficients];
            if (item.FlatnessLinearFrequencySignals.Count > 0) chirpAODWaveformProfile.FlatnessLinearFrequencySignals = [.. item.FlatnessLinearFrequencySignals];
            if (item.FlatnessTotalFrequencySignals.Count > 0) chirpAODWaveformProfile.FlatnessTotalFrequencySignals = [.. item.FlatnessTotalFrequencySignals];
            if (item.FlatnessAstigmatismCompensationSignals.Count > 0) chirpAODWaveformProfile.FlatnessAstigmatismCompensationSignals = [.. item.FlatnessAstigmatismCompensationSignals];
            if (item.FlatnessSphericalAberrationCompensationSignals.Count > 0) chirpAODWaveformProfile.FlatnessSphericalAberrationCompensationSignals = [.. item.FlatnessSphericalAberrationCompensationSignals];
            if (item.FlatnessSecondaryAstigmatismCompensationSignals.Count > 0) chirpAODWaveformProfile.FlatnessSecondaryAstigmatismCompensationSignals = [.. item.FlatnessSecondaryAstigmatismCompensationSignals];
            if (item.FlatnessComaCompensationSignals.Count > 0) chirpAODWaveformProfile.FlatnessComaCompensationSignals = [.. item.FlatnessComaCompensationSignals];
            if (item.FlatnessTrefoilCompensationSignals.Count > 0) chirpAODWaveformProfile.FlatnessTrefoilCompensationSignals = [.. item.FlatnessTrefoilCompensationSignals];
            if (item.FlatnessQuadrafoilCompensationSignals.Count > 0) chirpAODWaveformProfile.FlatnessQuadrafoilCompensationSignals = [.. item.FlatnessQuadrafoilCompensationSignals];
            if (item.FlatnessAlphaOrderCompensationSignals.Count > 0) chirpAODWaveformProfile.FlatnessAlphaOrderCompensationSignals = [.. item.FlatnessAlphaOrderCompensationSignals];

            result.Add(chirpAODWaveformProfile);
        }

        return result;
    }
}
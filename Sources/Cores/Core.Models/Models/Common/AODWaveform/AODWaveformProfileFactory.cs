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
        Guard.IsEqualTo(Path.GetExtension(prescanAODWaveformResultFilePath), AODWaveformGenerator1.PrescanAODWaveformFileExtension, "File Extension is not valid.");

        var prescanAODWaveformResult = FileHelper.DeserializeOperate<AODWaveformGenerator1.PrescanAODWaveformResult>(prescanAODWaveformResultFilePath);
        Guard.IsNotNull(prescanAODWaveformResult, "File Extension is not valid.");

        return CreatePrescanList(prescanAODWaveformResult);
    }

    public static IReadOnlyList<ChirpAODWaveformProfile> CreateChirpList(string chirpAODWaveformResultFilePath)
    {
        Guard.IsEqualTo(Path.GetExtension(chirpAODWaveformResultFilePath), AODWaveformGenerator1.ChirpAODWaveformFileExtension, "File Extension is not valid.");

        var chirpAODWaveformResult = FileHelper.DeserializeOperate<AODWaveformGenerator1.ChirpAODWaveformResult>(chirpAODWaveformResultFilePath);
        Guard.IsNotNull(chirpAODWaveformResult, "File Extension is not valid.");

        return CreateChirpList(chirpAODWaveformResult);
    }

    public static IReadOnlyList<PrescanAODWaveformProfile> CreatePrescanList(AODWaveformGenerator1.PrescanAODWaveformResult prescanAODWaveformResult, double coefficient = 1d)
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
            if (item.FlatnessC2CompensationSignals.Count > 0) prescanAODWaveformProfile.FlatnessC2CompensationSignals = [.. item.FlatnessC2CompensationSignals];
            if (item.FlatnessC3CompensationSignals.Count > 0) prescanAODWaveformProfile.FlatnessC3CompensationSignals = [.. item.FlatnessC3CompensationSignals];
            if (item.FlatnessC4CompensationSignals.Count > 0) prescanAODWaveformProfile.FlatnessC4CompensationSignals = [.. item.FlatnessC4CompensationSignals];
            if (item.FlatnessC5CompensationSignals.Count > 0) prescanAODWaveformProfile.FlatnessC5CompensationSignals = [.. item.FlatnessC5CompensationSignals];
            if (item.FlatnessC6CompensationSignals.Count > 0) prescanAODWaveformProfile.FlatnessC6CompensationSignals = [.. item.FlatnessC6CompensationSignals];
            if (item.FlatnessC7CompensationSignals.Count > 0) prescanAODWaveformProfile.FlatnessC7CompensationSignals = [.. item.FlatnessC7CompensationSignals];
            if (item.FlatnessC8CompensationSignals.Count > 0) prescanAODWaveformProfile.FlatnessC8CompensationSignals = [.. item.FlatnessC8CompensationSignals];

            result.Add(prescanAODWaveformProfile);
        }

        return result;
    }

    public static IReadOnlyList<ChirpAODWaveformProfile> CreateChirpList(AODWaveformGenerator1.ChirpAODWaveformResult chirpAODWaveformResult)
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
            if (item.FlatnessC2CompensationSignals.Count > 0) chirpAODWaveformProfile.FlatnessC2CompensationSignals = [.. item.FlatnessC2CompensationSignals];
            if (item.FlatnessC3CompensationSignals.Count > 0) chirpAODWaveformProfile.FlatnessC3CompensationSignals = [.. item.FlatnessC3CompensationSignals];
            if (item.FlatnessC4CompensationSignals.Count > 0) chirpAODWaveformProfile.FlatnessC4CompensationSignals = [.. item.FlatnessC4CompensationSignals];
            if (item.FlatnessC5CompensationSignals.Count > 0) chirpAODWaveformProfile.FlatnessC5CompensationSignals = [.. item.FlatnessC5CompensationSignals];
            if (item.FlatnessC6CompensationSignals.Count > 0) chirpAODWaveformProfile.FlatnessC6CompensationSignals = [.. item.FlatnessC6CompensationSignals];
            if (item.FlatnessC7CompensationSignals.Count > 0) chirpAODWaveformProfile.FlatnessC7CompensationSignals = [.. item.FlatnessC7CompensationSignals];
            if (item.FlatnessC8CompensationSignals.Count > 0) chirpAODWaveformProfile.FlatnessC8CompensationSignals = [.. item.FlatnessC8CompensationSignals];

            result.Add(chirpAODWaveformProfile);
        }

        return result;
    }
}
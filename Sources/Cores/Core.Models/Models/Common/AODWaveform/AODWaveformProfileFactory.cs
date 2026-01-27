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
            if (item.FlatnessTotalPhaseSignals.Count > 0) prescanAODWaveformProfile.FlatnessTotalPhaseSignals = [.. item.FlatnessTotalPhaseSignals];
            if (item.FlatnessTotalCompensationFrequencySignals.Count > 0) prescanAODWaveformProfile.FlatnessTotalCompensationFrequencySignals = [.. item.FlatnessTotalCompensationFrequencySignals];
            if (item.FlatnessTotalCompensationPhaseSignals.Count > 0) prescanAODWaveformProfile.FlatnessTotalCompensationPhaseSignals = [.. item.FlatnessTotalCompensationPhaseSignals];
            if (item.FlatnessP3CompensationFrequencySignals.Count > 0) prescanAODWaveformProfile.FlatnessP3CompensationFrequencySignals = [.. item.FlatnessP3CompensationFrequencySignals];
            if (item.FlatnessP3CompensationPhaseSignals.Count > 0) prescanAODWaveformProfile.FlatnessP3CompensationPhaseSignals = [.. item.FlatnessP3CompensationPhaseSignals];
            if (item.FlatnessP4CompensationFrequencySignals.Count > 0) prescanAODWaveformProfile.FlatnessP4CompensationFrequencySignals = [.. item.FlatnessP4CompensationFrequencySignals];
            if (item.FlatnessP4CompensationPhaseSignals.Count > 0) prescanAODWaveformProfile.FlatnessP4CompensationPhaseSignals = [.. item.FlatnessP4CompensationPhaseSignals];
            if (item.FlatnessP5CompensationFrequencySignals.Count > 0) prescanAODWaveformProfile.FlatnessP5CompensationFrequencySignals = [.. item.FlatnessP5CompensationFrequencySignals];
            if (item.FlatnessP5CompensationPhaseSignals.Count > 0) prescanAODWaveformProfile.FlatnessP5CompensationPhaseSignals = [.. item.FlatnessP5CompensationPhaseSignals];
            if (item.FlatnessP6CompensationFrequencySignals.Count > 0) prescanAODWaveformProfile.FlatnessP6CompensationFrequencySignals = [.. item.FlatnessP6CompensationFrequencySignals];
            if (item.FlatnessP6CompensationPhaseSignals.Count > 0) prescanAODWaveformProfile.FlatnessP6CompensationPhaseSignals = [.. item.FlatnessP6CompensationPhaseSignals];
            if (item.FlatnessP7CompensationFrequencySignals.Count > 0) prescanAODWaveformProfile.FlatnessP7CompensationFrequencySignals = [.. item.FlatnessP7CompensationFrequencySignals];
            if (item.FlatnessP7CompensationPhaseSignals.Count > 0) prescanAODWaveformProfile.FlatnessP7CompensationPhaseSignals = [.. item.FlatnessP7CompensationPhaseSignals];
            if (item.FlatnessP8CompensationFrequencySignals.Count > 0) prescanAODWaveformProfile.FlatnessP8CompensationFrequencySignals = [.. item.FlatnessP8CompensationFrequencySignals];
            if (item.FlatnessP8CompensationPhaseSignals.Count > 0) prescanAODWaveformProfile.FlatnessP8CompensationPhaseSignals = [.. item.FlatnessP8CompensationPhaseSignals];

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
            if (item.FlatnessTotalPhaseSignals.Count > 0) chirpAODWaveformProfile.FlatnessTotalPhaseSignals = [.. item.FlatnessTotalPhaseSignals];
            if (item.FlatnessTotalCompensationFrequencySignals.Count > 0) chirpAODWaveformProfile.FlatnessTotalCompensationFrequencySignals = [.. item.FlatnessTotalCompensationFrequencySignals];
            if (item.FlatnessTotalCompensationPhaseSignals.Count > 0) chirpAODWaveformProfile.FlatnessTotalCompensationPhaseSignals = [.. item.FlatnessTotalCompensationPhaseSignals];
            if (item.FlatnessP3CompensationFrequencySignals.Count > 0) chirpAODWaveformProfile.FlatnessP3CompensationFrequencySignals = [.. item.FlatnessP3CompensationFrequencySignals];
            if (item.FlatnessP3CompensationPhaseSignals.Count > 0) chirpAODWaveformProfile.FlatnessP3CompensationPhaseSignals = [.. item.FlatnessP3CompensationPhaseSignals];
            if (item.FlatnessP4CompensationFrequencySignals.Count > 0) chirpAODWaveformProfile.FlatnessP4CompensationFrequencySignals = [.. item.FlatnessP4CompensationFrequencySignals];
            if (item.FlatnessP4CompensationPhaseSignals.Count > 0) chirpAODWaveformProfile.FlatnessP4CompensationPhaseSignals = [.. item.FlatnessP4CompensationPhaseSignals];
            if (item.FlatnessP5CompensationFrequencySignals.Count > 0) chirpAODWaveformProfile.FlatnessP5CompensationFrequencySignals = [.. item.FlatnessP5CompensationFrequencySignals];
            if (item.FlatnessP5CompensationPhaseSignals.Count > 0) chirpAODWaveformProfile.FlatnessP5CompensationPhaseSignals = [.. item.FlatnessP5CompensationPhaseSignals];
            if (item.FlatnessP6CompensationFrequencySignals.Count > 0) chirpAODWaveformProfile.FlatnessP6CompensationFrequencySignals = [.. item.FlatnessP6CompensationFrequencySignals];
            if (item.FlatnessP6CompensationPhaseSignals.Count > 0) chirpAODWaveformProfile.FlatnessP6CompensationPhaseSignals = [.. item.FlatnessP6CompensationPhaseSignals];
            if (item.FlatnessP7CompensationFrequencySignals.Count > 0) chirpAODWaveformProfile.FlatnessP7CompensationFrequencySignals = [.. item.FlatnessP7CompensationFrequencySignals];
            if (item.FlatnessP7CompensationPhaseSignals.Count > 0) chirpAODWaveformProfile.FlatnessP7CompensationPhaseSignals = [.. item.FlatnessP7CompensationPhaseSignals];
            if (item.FlatnessP8CompensationFrequencySignals.Count > 0) chirpAODWaveformProfile.FlatnessP8CompensationFrequencySignals = [.. item.FlatnessP8CompensationFrequencySignals];
            if (item.FlatnessP8CompensationPhaseSignals.Count > 0) chirpAODWaveformProfile.FlatnessP8CompensationPhaseSignals = [.. item.FlatnessP8CompensationPhaseSignals];

            result.Add(chirpAODWaveformProfile);
        }

        return result;
    }
}
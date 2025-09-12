using CommunityToolkit.Diagnostics;
using Core.Models.Enums.Optics;
using System.IO;

namespace Core.Models.Models.Common.AODWaveform;

public static class AODWaveformResultFactory
{
    public static PrescanAODWaveformResult CreatePrescan(OpticsAODElectrodeEnum opticsAODElectrodeEnum, string filePath)
    {
        Guard.IsTrue(File.Exists(filePath), $"File not found: {filePath}");

        return new PrescanAODWaveformResult
        {
            OpticsAODElectrodeEnum = opticsAODElectrodeEnum,
            FilePath = filePath
        };
    }

    public static IReadOnlyList<PrescanAODWaveformResult> CreatePrescanList(IReadOnlyList<PrescanAODWaveformProfile> prescanAODWaveformProfileList, string? directoryPath = null) => prescanAODWaveformProfileList
        .Select(t => directoryPath is null ? t.AdaptTo() : t.AdaptTo(directoryPath))
        .ToArray();

    public static ChirpAODWaveformResult CreateChirp(OpticsAODElectrodeEnum opticsAODElectrodeEnum, string filePath)
    {
        Guard.IsTrue(File.Exists(filePath), $"File not found: {filePath}");

        return new ChirpAODWaveformResult
        {
            OpticsAODElectrodeEnum = opticsAODElectrodeEnum,
            FilePath = filePath
        };
    }

    public static IReadOnlyList<ChirpAODWaveformResult> CreateChirpList(IReadOnlyList<ChirpAODWaveformProfile> chirpAODWaveformProfileList, string? directoryPath = null) => chirpAODWaveformProfileList
        .Select(t => directoryPath is null ? t.AdaptTo() : t.AdaptTo(directoryPath))
        .ToArray();
}
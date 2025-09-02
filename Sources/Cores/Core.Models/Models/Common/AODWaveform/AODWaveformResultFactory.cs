using Core.Models.Enums.Optics;

namespace Core.Models.Models.Common.AODWaveform;

public static class AODWaveformResultFactory
{
    public static PrescanAODWaveformResult CreatePrescan(OpticsAODElectrodeEnum opticsAODElectrodeEnum, string filePath) => new()
    {
        OpticsAODElectrodeEnum = opticsAODElectrodeEnum,
        FilePath = filePath
    };

    public static IReadOnlyList<PrescanAODWaveformResult> CreatePrescanList(IReadOnlyList<PrescanAODWaveformProfile> prescanAODWaveformProfileList, string? directoryPath = null) =>
        prescanAODWaveformProfileList.Select(t => directoryPath is null ? t.AdaptTo() : t.AdaptTo(directoryPath)).ToList();

    public static ChirpAODWaveformResult CreateChirp(OpticsAODElectrodeEnum opticsAODElectrodeEnum, string filePath) => new()
    {
        OpticsAODElectrodeEnum = opticsAODElectrodeEnum,
        FilePath = filePath
    };

    public static IReadOnlyList<ChirpAODWaveformResult> CreateChirpList(IReadOnlyList<ChirpAODWaveformProfile> chirpAODWaveformProfileList, string? directoryPath = null) =>
        chirpAODWaveformProfileList.Select(t => directoryPath is null ? t.AdaptTo() : t.AdaptTo(directoryPath)).ToList();
}
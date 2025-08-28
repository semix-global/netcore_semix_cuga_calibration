using Core.Models.Enums.Optics;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Common.AODWaveform;

public static class AODWaveformProfileFactory
{
    public static PrescanAODWaveformProfile CreatePrescan(OpticsAODElectrodeEnum opticsAODElectrodeEnum, string filePath, double coefficient = 1d, int? customZeroSampleCount = null)
    {
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
}
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Common.AODWaveform;

public class ChirpAODWaveformResult : AODWaveformResult, IAdaptTo<ChirpAODWaveformProfile>, ICloneable<ChirpAODWaveformResult>
{
    internal ChirpAODWaveformResult()
    {
    }

    ChirpAODWaveformProfile IAdaptTo<ChirpAODWaveformProfile>.AdaptTo() => AODWaveformProfileFactory.CreateChirp(OpticsAODElectrodeEnum, FilePath);

    public ChirpAODWaveformResult Clone() => new()
    {
        OpticsAODElectrodeEnum = OpticsAODElectrodeEnum,
        FilePath = FilePath
    };
}
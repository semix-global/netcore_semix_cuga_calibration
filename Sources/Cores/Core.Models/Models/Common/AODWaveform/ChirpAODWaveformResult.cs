using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Common.AODWaveform;

public sealed class ChirpAODWaveformResult : AbstractAODWaveformResult<ChirpAODWaveformResult>, IAdaptTo<ChirpAODWaveformProfile>, ICloneable<ChirpAODWaveformResult>
{
    internal ChirpAODWaveformResult()
    {
    }

    ChirpAODWaveformProfile IAdaptTo<ChirpAODWaveformProfile>.AdaptTo() => AODWaveformProfileFactory.CreateChirp(OpticsAODElectrodeEnum, FilePath);

    public ChirpAODWaveformResult Clone() => AdaptIn(new ChirpAODWaveformResult());
}
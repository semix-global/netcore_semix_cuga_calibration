using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Common.AODWaveform;

public sealed class ChirpAODWaveformResult : AbstractAODWaveformResult, IAdaptTo<ChirpAODWaveformProfile>, ICloneable<ChirpAODWaveformResult>
{
    public static readonly ChirpAODWaveformResult Default = new();

    internal ChirpAODWaveformResult()
    {
    }

    ChirpAODWaveformProfile IAdaptTo<ChirpAODWaveformProfile>.AdaptTo() => AODWaveformProfileFactory.CreateChirp(OpticsAODElectrodeEnum, FilePath);

    public ChirpAODWaveformResult Clone() => AdaptIn(new ChirpAODWaveformResult());
}
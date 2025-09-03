using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Common.AODWaveform;

public sealed class ChirpAODWaveformProfile : AbstractAODWaveformProfile<ChirpAODWaveformProfile>, IAdaptTo<ChirpAODWaveformResult>, ICloneable<ChirpAODWaveformProfile>
{
    internal ChirpAODWaveformProfile()
    {
    }

    public ChirpAODWaveformResult AdaptTo() => AODWaveformResultFactory.CreateChirp(OpticsAODElectrodeEnum, FilePath);

    public ChirpAODWaveformResult AdaptTo(string directoryPath) => AODWaveformResultFactory.CreateChirp(OpticsAODElectrodeEnum, Save(directoryPath));

    public ChirpAODWaveformProfile Clone() => AdaptIn(new ChirpAODWaveformProfile());
}
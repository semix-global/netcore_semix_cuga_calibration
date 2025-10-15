using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Common.AODWaveform;

public sealed class ChirpAODWaveformProfile :
    AbstractAODWaveformProfile,
    IAdaptTo<ChirpAODWaveformResult>,
    ICloneable<ChirpAODWaveformProfile>
{
    public static readonly ChirpAODWaveformProfile Default = new();

    internal ChirpAODWaveformProfile()
    {
    }

    public ChirpAODWaveformResult AdaptTo() => AODWaveformResultFactory.CreateChirp(OpticsAODElectrodeEnum, FilePath);

    public ChirpAODWaveformResult AdaptTo(string directoryPath) => AODWaveformResultFactory.CreateChirp(OpticsAODElectrodeEnum, Save(directoryPath));

    public ChirpAODWaveformProfile Clone() => (ChirpAODWaveformProfile)new ChirpAODWaveformProfile().AdaptIn(this);
}
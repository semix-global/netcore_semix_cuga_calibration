using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Common.AODWaveform;

public sealed class ChirpAODWaveformProfile : AODWaveformProfile, IAdaptTo<ChirpAODWaveformResult>, ICloneable<ChirpAODWaveformProfile>
{
    internal ChirpAODWaveformProfile()
    {
    }

    public ChirpAODWaveformResult AdaptTo() => AODWaveformResultFactory.CreateChirp(OpticsAODElectrodeEnum, FilePath);

    public ChirpAODWaveformResult AdaptTo(string directoryPath) => AODWaveformResultFactory.CreateChirp(OpticsAODElectrodeEnum, Save(directoryPath));

    public ChirpAODWaveformProfile Clone() => new()
    {
        OpticsAODElectrodeEnum = OpticsAODElectrodeEnum,
        FilePath = FilePath,
        ZeroSampleCount = ZeroSampleCount,
        OffsetFrequency = OffsetFrequency,
        OffsetFrequencyPeriodMultiple = OffsetFrequencyPeriodMultiple,
        ShortList = [.. ShortList],
        ByteList = [.. ByteList]
    };
}
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Common.AODWaveform;

public sealed class ChirpAODWaveformProfile : AODWaveformProfile, ICloneable<ChirpAODWaveformProfile>
{
    public ChirpAODWaveformProfile Clone() => new()
    {
        OpticsAODElectrodeEnum = OpticsAODElectrodeEnum,
        FilePath = FilePath,
        ZeroSampleCount = ZeroSampleCount,
        ShortList = [.. ShortList],
        ByteList = [.. ByteList]
    };
}
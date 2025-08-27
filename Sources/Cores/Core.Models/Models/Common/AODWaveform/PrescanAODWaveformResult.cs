using Core.Wcf.Models.Laser;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Common.AODWaveform;

public class PrescanAODWaveformResult : AODWaveformResult, IAdaptTo<PrescanAODWaveformProfile>, ICloneable<PrescanAODWaveformResult>
{
    internal PrescanAODWaveformResult()
    {
    }

    public PrescanAODWaveformProfile AdaptTo() => AODWaveformProfileFactory.CreatePrescan(OpticsAODElectrodeEnum, FilePath);
    
    public PrescanAODWaveformResult Clone() => new()
    {
        OpticsAODElectrodeEnum = OpticsAODElectrodeEnum,
        FilePath = FilePath
    };


}
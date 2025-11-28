using Core.Wcf.Models.Laser;
using Net.Utilities.Mapper.Interfaces;

#if NETFRAMEWORK
using Core.Models.Extensions;
#endif

namespace Core.Models.Models.Common.AODWaveform;

public sealed class ChirpAODWaveformResult :
    AbstractAODWaveformResult,
    IAdaptTo<ChirpAODWaveformProfile>,
    IAdaptTo<CalibrationChirpAODWaveformResult>,
    ICloneable<ChirpAODWaveformResult>
{
    public static readonly ChirpAODWaveformResult Default = new();

    internal ChirpAODWaveformResult()
    {
    }

    ChirpAODWaveformProfile IAdaptTo<ChirpAODWaveformProfile>.AdaptTo() => AODWaveformProfileFactory.CreateChirp(OpticsAODElectrodeEnum, FilePath);

    CalibrationChirpAODWaveformResult IAdaptTo<CalibrationChirpAODWaveformResult>.AdaptTo() => new()
    {
#if NETFRAMEWORK
        OpticsAODElectrodeEnum = OpticsAODElectrodeEnum.ToCgAwgElectrodeEnum(),
#else
        OpticsAODElectrodeEnum = (int)OpticsAODElectrodeEnum,
#endif
        FilePath = FilePath
    };

    public ChirpAODWaveformResult Clone() => AdaptIn(new ChirpAODWaveformResult());
}
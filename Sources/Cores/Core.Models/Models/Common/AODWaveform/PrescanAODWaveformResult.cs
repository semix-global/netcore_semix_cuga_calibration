using Core.Models.Extensions;
using Core.Wcf.Models.Laser;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Common.AODWaveform;

public sealed class PrescanAODWaveformResult :
    AbstractAODWaveformResult,
    IAdaptTo<PrescanAODWaveformProfile>,
    IAdaptTo<CalibrationPrescanAODWaveformResult>,
    ICloneable<PrescanAODWaveformResult>
{
    public static readonly PrescanAODWaveformResult Default = new();

    internal PrescanAODWaveformResult()
    {
    }

    PrescanAODWaveformProfile IAdaptTo<PrescanAODWaveformProfile>.AdaptTo() => AODWaveformProfileFactory.CreatePrescan(OpticsAODElectrodeEnum, FilePath);

    CalibrationPrescanAODWaveformResult IAdaptTo<CalibrationPrescanAODWaveformResult>.AdaptTo() => new()
    {
#if NETFRAMEWORK
        OpticsAODElectrodeEnum = OpticsAODElectrodeEnum.ToCgAwgElectrodeEnum(),
#else
        OpticsAODElectrodeEnum = (int)OpticsAODElectrodeEnum,
#endif
        FilePath = FilePath
    };

    public PrescanAODWaveformResult Clone() => AdaptIn(new PrescanAODWaveformResult());
}
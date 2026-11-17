using Core.Wcf.Models.Laser;
using Net.Utilities.Mapper.Interfaces;
using Core.Models.Extensions;

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
        OpticsAODElectrodeEnum = OpticsAODElectrodeEnum.ToCgAwgElectrodeEnum(),
        FilePath = FilePath
    };

    public PrescanAODWaveformResult Clone() => AdaptIn(new PrescanAODWaveformResult());
}
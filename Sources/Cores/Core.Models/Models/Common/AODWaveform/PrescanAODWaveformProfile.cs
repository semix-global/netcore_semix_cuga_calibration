using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Common.AODWaveform;

public sealed class PrescanAODWaveformProfile : AODWaveformProfile, ICloneable<PrescanAODWaveformProfile>
{
    public PrescanAODWaveformProfile Clone() => new()
    {
        OpticsAODElectrodeEnum = OpticsAODElectrodeEnum,
        FilePath = FilePath,
        ZeroSampleCount = ZeroSampleCount,
        ShortList = [.. ShortList],
        ByteList = [.. ByteList]
    };

    public PrescanAODWaveformProfile ApplyCoefficient(double coefficient)
    {
        SetByteList(coefficient);

        return this;
    }

    public PrescanAODWaveformProfile ApplyCoefficientWindowList(IReadOnlyList<double> coefficientWindowList)
    {
        SetByteList(coefficientWindowList);

        return this;
    }
}
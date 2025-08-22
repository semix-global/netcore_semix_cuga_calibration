using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Common.AODWaveform;

public sealed class PrescanAODWaveformProfile : AODWaveformProfile, IAdaptTo<PrescanAODWaveformResult>, ICloneable<PrescanAODWaveformProfile>
{
    internal PrescanAODWaveformProfile()
    {
    }

    public PrescanAODWaveformResult AdaptTo() => AODWaveformResultFactory.CreatePrescan(OpticsAODElectrodeEnum, FilePath);

    public PrescanAODWaveformResult AdaptTo(string directoryPath)=> AODWaveformResultFactory.CreatePrescan(OpticsAODElectrodeEnum, Save(directoryPath));

    public PrescanAODWaveformProfile Clone() => new()
    {
        OpticsAODElectrodeEnum = OpticsAODElectrodeEnum,
        FilePath = FilePath,
        ZeroSampleCount = ZeroSampleCount,
        OffsetFrequency = OffsetFrequency,
        OffsetFrequencyPeriodMultiple = OffsetFrequencyPeriodMultiple,
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
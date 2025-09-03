using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Common.AODWaveform;

public sealed class PrescanAODWaveformProfile : AbstractAODWaveformProfile<PrescanAODWaveformProfile>, IAdaptTo<PrescanAODWaveformResult>, ICloneable<PrescanAODWaveformProfile>
{
    internal PrescanAODWaveformProfile()
    {
    }

    public PrescanAODWaveformResult AdaptTo() => AODWaveformResultFactory.CreatePrescan(OpticsAODElectrodeEnum, FilePath);

    public PrescanAODWaveformResult AdaptTo(string directoryPath) => AODWaveformResultFactory.CreatePrescan(OpticsAODElectrodeEnum, Save(directoryPath));

    public PrescanAODWaveformProfile Clone() => AdaptIn(new PrescanAODWaveformProfile());

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
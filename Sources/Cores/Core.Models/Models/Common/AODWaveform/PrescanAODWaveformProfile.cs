using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Common.AODWaveform;

public sealed class PrescanAODWaveformProfile : AbstractAODWaveformProfile, IAdaptTo<PrescanAODWaveformResult>, ICloneable<PrescanAODWaveformProfile>
{
    public static readonly PrescanAODWaveformProfile Default = new();

    internal PrescanAODWaveformProfile()
    {
    }

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

    public PrescanAODWaveformResult AdaptTo() => AODWaveformResultFactory.CreatePrescan(OpticsAODElectrodeEnum, FilePath);

    public PrescanAODWaveformResult AdaptTo(string directoryPath) => AODWaveformResultFactory.CreatePrescan(OpticsAODElectrodeEnum, Save(directoryPath));

    public PrescanAODWaveformProfile Clone() => AdaptIn(new PrescanAODWaveformProfile());
}
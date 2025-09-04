using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Utilities;
using Local.NoSQL.DB.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Common.AODWaveform.Generates;

public sealed partial class GenerateAODWaveformElectrodeConfiguration :
    ObservableCacheBase,
    IAdaptTo<AODWaveformGenerator.AODWaveformOffsetConfiguration>,
    IAdaptIn<AbstractAODWaveformProfile, GenerateAODWaveformElectrodeConfiguration>
{
    [ObservableProperty]
    private OpticsAODElectrodeEnum _opticsAODElectrodeEnum;

    [ObservableProperty]
    private double _offsetFrequency;

    [ObservableProperty]
    private double _offsetFrequencyPeriodMultiple;

    public AODWaveformGenerator.AODWaveformOffsetConfiguration AdaptTo() => new(OpticsAODElectrodeEnum.ToString(), OffsetFrequency, OffsetFrequencyPeriodMultiple);

    public GenerateAODWaveformElectrodeConfiguration AdaptIn(AbstractAODWaveformProfile obj)
    {
        OpticsAODElectrodeEnum = obj.OpticsAODElectrodeEnum;
        OffsetFrequency = obj.OffsetFrequency;
        OffsetFrequencyPeriodMultiple = obj.OffsetFrequencyPeriodMultiple;

        return this;
    }
}
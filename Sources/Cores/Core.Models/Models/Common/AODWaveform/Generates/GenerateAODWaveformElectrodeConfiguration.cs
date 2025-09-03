using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Utilities;
using Local.NoSQL.DB.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Common.AODWaveform.Generates;

public partial class GenerateAODWaveformElectrodeConfiguration :
    ObservableCacheBase,
    IAdaptTo<AODWaveformGenerator.AODWaveformConfiguration>,
    IAdaptIn<PrescanAODWaveformProfile, GenerateAODWaveformElectrodeConfiguration>,
    IAdaptIn<ChirpAODWaveformProfile, GenerateAODWaveformElectrodeConfiguration>
{
    [ObservableProperty]
    private OpticsAODElectrodeEnum _opticsAODElectrodeEnum;

    [ObservableProperty]
    private double _offsetFrequency;

    [ObservableProperty]
    private double _offsetFrequencyPeriodMultiple;

    private GenerateAODWaveformElectrodeConfiguration AdaptIn<T>(AbstractAODWaveformProfile<T> obj) where T : AbstractAODWaveformProfile<T>
    {
        OpticsAODElectrodeEnum = obj.OpticsAODElectrodeEnum;
        OffsetFrequency = obj.OffsetFrequency;
        OffsetFrequencyPeriodMultiple = obj.OffsetFrequencyPeriodMultiple;

        return this;
    }

    public AODWaveformGenerator.AODWaveformConfiguration AdaptTo() => new(OpticsAODElectrodeEnum.ToString(), OffsetFrequency, OffsetFrequencyPeriodMultiple);

    public GenerateAODWaveformElectrodeConfiguration AdaptIn(PrescanAODWaveformProfile obj) => AdaptIn<PrescanAODWaveformProfile>(obj);

    public GenerateAODWaveformElectrodeConfiguration AdaptIn(ChirpAODWaveformProfile obj) => AdaptIn<ChirpAODWaveformProfile>(obj);
}
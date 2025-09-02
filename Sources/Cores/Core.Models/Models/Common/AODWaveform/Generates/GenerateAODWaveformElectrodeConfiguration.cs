using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Utilities;
using Local.NoSQL.DB.Providers.Bases;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Common.AODWaveform.Generates;

public partial class GenerateAODWaveformElectrodeConfiguration : ObservableCacheBase, IAdaptTo<AODWaveformGenerator.AODWaveformConfiguration>
{
    [ObservableProperty]
    private OpticsAODElectrodeEnum _opticsAODElectrodeEnum;

    [ObservableProperty]
    private double _offsetFrequency;

    [ObservableProperty]
    private double _offsetFrequencyPeriodMultiple;

    public AODWaveformGenerator.AODWaveformConfiguration AdaptTo() => new(EnumHelper.ToDescriptionString(OffsetFrequency), OffsetFrequency, OffsetFrequencyPeriodMultiple);
}
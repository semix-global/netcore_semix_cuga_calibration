using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.AODWaveform.Generates;
using Core.Utilities.SourceGenerators.Attributes;
using Local.SQL.Cache.Providers.Extensions;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.WPF.Enums;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public sealed class ChirpGenerateAODWaveformCache : GenerateAODWaveformCache<GenerateChirpAODWaveformParam, ChirpAODWaveformProfile>;

[IOCAppService(ServiceType = typeof(ChirpGenerateAODWaveformWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed class ChirpGenerateAODWaveformWindowViewModel : AbstractGenerateAODWaveformWindowViewModel<ChirpGenerateAODWaveformCache, GenerateChirpAODWaveformParam, ChirpAODWaveformProfile>
{
    public override string Name => "Generate Chirp AOD Waveform";

    [DefaultCache]
    public override ChirpGenerateAODWaveformCache Cache
    {
        get;
        set => SetProperty(ref field, value);
    } = new();

    protected override void LoadedElectrodeOffsetResult(CancellationToken cancellationToken)
    {
        var chirpAODWaveformElectrodeInitializeCache = CacheProvider.GetOrDefault<ChirpAODWaveformElectrodeOffsetCache>();
        if (chirpAODWaveformElectrodeInitializeCache.ElectrodeConfigurationResults.Count == 0)
        {
            DialogWindowProvider.ShowDialog("Please initialize the chirp electrode configuration as it is currently empty.", DialogButtonsEnum.OK, DialogIconEnum.Warning);

            return;
        }

        Cache.Param.ElectrodeConfigurations = chirpAODWaveformElectrodeInitializeCache.ElectrodeConfigurationResults;
    }

    protected override void GenerateAODWaveform(CancellationToken cancellationToken)
    {
        Cache.Profiles = [];
        Cache.AODWaveformResultFilePath = string.Empty;

        var aodWaveformResult = AODWaveformGenerator1.GenerateChirpAODWaveform(Cache.Param.AdaptTo(), cancellationToken);

        Cache.Profiles = AODWaveformProfileFactory.CreateChirpList(aodWaveformResult);
        Cache.AODWaveformResultFilePath = aodWaveformResult.FilePath;
    }

    protected override void SetAODWaveformProfiles(CancellationToken cancellationToken) => LaserViewModel.SetChirpAODWaveProfiles(Cache.Param.ProductivityInformation.OpticsIlluminationModeEnum, Cache.Profiles);
}
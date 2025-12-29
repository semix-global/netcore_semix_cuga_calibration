using CommunityToolkit.Diagnostics;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.AODWaveform.Generates;
using Local.NoSQL.DB.Providers.Extensions;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models;
using Net.Utilities.WPF.Enums;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

[IOCAppService(ServiceType = typeof(ChirpGenerateAODWaveformWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed class ChirpGenerateAODWaveformWindowViewModel : AbstractGenerateAODWaveformWindowViewModel<GenerateChirpAODWaveformParam, ChirpAODWaveformProfile>
{
    public override string Name => "Generate Chirp AOD Waveform";

    protected override void LoadedElectrodeOffsetResult(CancellationToken cancellationToken)
    {
        var chirpAODWaveformElectrodeInitializeCache = CacheProvider.GetOrDefault<ChirpAODWaveformElectrodeInitializeCache>();
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

        var (aodWaveformResult, exception) = AODWaveformGenerator.GenerateChirpAODWaveform(Cache.Param.AdaptTo(), cancellationToken);
        if (aodWaveformResult.IsSuccess == false) ThrowHelper.ThrowInvalidOperationException(string.Empty, GuardUtils.IsNotNullAndReturn(exception));

        Cache.Profiles = AODWaveformProfileFactory.CreateChirpList(aodWaveformResult);
        Cache.AODWaveformResultFilePath = aodWaveformResult.FilePath;
    }

    protected override void SetAODWaveformProfiles(CancellationToken cancellationToken) => LaserViewModel.SetChirpAODWaveProfiles(Cache.Param.ProductivityInformation.OpticsIlluminationModeEnum, Cache.Profiles);
}
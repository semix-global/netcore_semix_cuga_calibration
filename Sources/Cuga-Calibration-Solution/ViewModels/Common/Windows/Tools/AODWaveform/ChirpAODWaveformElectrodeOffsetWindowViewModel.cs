using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Optics;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.AODWaveform.Generates;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Common.Pattern;
using Core.Utilities;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public sealed partial class ChirpAODWaveformElectrodeOffsetCache : AODWaveformElectrodeOffsetCache<ChirpAODWaveformElectrodeOffsetItem>
{
    [ObservableProperty]
    private string _prescanAODWaveformResultFilePath = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<PrescanAODWaveformProfile> _prescanAODWaveformProfiles = [];

    public override object ToHtmlAnonymous() => new HtmlQuote(new
    {
        Base = new HtmlBullet(base.ToHtmlAnonymous()),
        PrescanAODWaveformResultFilePath,
        PrescanAODWaveformProfiles = new HtmlTable([.. PrescanAODWaveformProfiles.Select(t => t.ToHtmlAnonymous())])
    });
}

public sealed partial class ChirpAODWaveformElectrodeOffsetItem : AODWaveformElectrodeOffsetItem
{
    [ObservableProperty]
    private string _chirpAODWaveformResultFilePath = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<ChirpAODWaveformProfile> _chirpAODWaveformProfiles = [];

    public override object ToHtmlAnonymous() => new HtmlQuote(new
    {
        Base = new HtmlBullet(base.ToHtmlAnonymous()),
        ChirpAODWaveformResultFilePath,
        ChirpAODWaveformProfiles = new HtmlTable([.. ChirpAODWaveformProfiles.Select(t => t.ToHtmlAnonymous())])
    });
}

[IOCAppService(ServiceType = typeof(ChirpAODWaveformElectrodeOffsetWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public partial class ChirpAODWaveformElectrodeOffsetWindowViewModel : AbstractAODWaveformElectrodeOffsetWindowViewModel<ChirpAODWaveformElectrodeOffsetCache, ChirpAODWaveformElectrodeOffsetItem>
{
    protected override string Name => "Chirp AOD Waveform Electrode Offset";

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task Step1Async(CancellationToken cancellationToken)
    {
        await InvokeAsync("Step1 Generate Prescan AOD Waveform", () =>
        {
            Cache.GenerateChirpAODWaveformParam.Amplitude = Cache.DefaultAmplitude;
            Cache.GenerateChirpAODWaveformParam.DirectoryPath = AODWaveformDirectoryPath;
            var (aodWaveformResult, exception) = AODWaveformGenerator.GeneratePrescanAODWaveform(Cache.GeneratePrescanAODWaveformParam.AdaptTo(), cancellationToken);
            if (aodWaveformResult.IsSuccess == false) throw GuardUtils.IsNotNullAndReturn(exception);

            Cache.PrescanAODWaveformProfiles = AODWaveformProfileFactory.CreatePrescanList(aodWaveformResult);
            Cache.PrescanAODWaveformResultFilePath = aodWaveformResult.FilePath;

            Logger.LogHtmlHeaderIsOk(HtmlHeaderLevelEnum.Header3, new HtmlBullet(Cache.ToHtmlAnonymous()), HtmlLogUniqueId.LoggingHtml());

            return Task.FromResult(true);
        });
    }

    protected override void SetAODWaveform(ChirpAODWaveformElectrodeOffsetCache cache, ChirpAODWaveformElectrodeOffsetItem item, CancellationToken cancellationToken)
    {
        Cache.GenerateChirpAODWaveformParam.WithFrequencyFlatness(item.Frequency);
        Cache.GenerateChirpAODWaveformParam.Amplitude = Cache.DefaultAmplitude;
        Cache.GenerateChirpAODWaveformParam.DirectoryPath = AODWaveformDirectoryPath;
        Cache.GenerateChirpAODWaveformParam.ElectrodeConfigurations =
        [
            new GenerateAODWaveformElectrodeConfiguration
            {
                OpticsAODElectrodeEnum = OpticsAODElectrodeEnum.Electrode1,
                OffsetFrequency = Cache.OffsetFrequency,
                OffsetFrequencyPeriodCoefficient = 0
            },
            new GenerateAODWaveformElectrodeConfiguration
            {
                OpticsAODElectrodeEnum = OpticsAODElectrodeEnum.Electrode2,
                OffsetFrequency = Cache.OffsetFrequency,
                OffsetFrequencyPeriodCoefficient = item.OffsetFrequencyPeriodCoefficient
            }
        ];
        var (aodWaveformResult, exception) = AODWaveformGenerator.GenerateChirpAODWaveform(cache.GenerateChirpAODWaveformParam.AdaptTo(), cancellationToken);
        if (aodWaveformResult.IsSuccess == false) throw GuardUtils.IsNotNullAndReturn(exception);

        item.ChirpAODWaveformProfiles = AODWaveformProfileFactory.CreateChirpList(aodWaveformResult);
        item.ChirpAODWaveformResultFilePath = aodWaveformResult.FilePath;

        LaserViewModel.SetPrescanAODWaveProfiles(cache.PrescanAODWaveformProfiles);
        LaserViewModel.SetChirpAODWaveProfiles(item.ChirpAODWaveformProfiles);
    }
}
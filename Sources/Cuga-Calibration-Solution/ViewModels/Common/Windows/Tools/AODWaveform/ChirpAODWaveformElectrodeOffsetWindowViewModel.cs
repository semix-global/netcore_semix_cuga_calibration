
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.AODWaveform.Generates;
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
    private double _prescanFrequency;

    [ObservableProperty]
    private string _prescanAODWaveformResultFilePath = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<PrescanAODWaveformProfile> _prescanAODWaveformProfiles = [];

    public override object ToHtmlAnonymous() => new
    {
        ChirpParam = new HtmlQuote(base.ToHtmlAnonymous()),
        PrescanAODWaveformResultFilePath,
        PrescanAODWaveformProfiles = new HtmlTable([.. PrescanAODWaveformProfiles.Select(t => t.ToHtmlAnonymous())])
    };
}

public sealed partial class ChirpAODWaveformElectrodeOffsetItem : AODWaveformElectrodeOffsetItem
{
    [ObservableProperty]
    private string _chirpAODWaveformResultFilePath = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<ChirpAODWaveformProfile> _chirpAODWaveformProfiles = [];

    public override object ToHtmlAnonymous() => new
    {
        Base = new HtmlQuote(base.ToHtmlAnonymous()),
        ChirpAODWaveformResultFilePath,
        ChirpAODWaveformProfiles = new HtmlTable([.. ChirpAODWaveformProfiles.Select(t => t.ToHtmlAnonymous())])
    };
}

[IOCAppService(ServiceType = typeof(ChirpAODWaveformElectrodeOffsetWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public class ChirpAODWaveformElectrodeOffsetWindowViewModel : AbstractAODWaveformElectrodeOffsetWindowViewModel<ChirpAODWaveformElectrodeOffsetCache, ChirpAODWaveformElectrodeOffsetItem>
{
    public override string Name => "Chirp AOD Waveform Electrode Offset";

    protected override void GenerateFixedAODWaveform(CancellationToken cancellationToken)
    {
        Cache.PrescanAODWaveformProfiles = [];
        Cache.PrescanAODWaveformResultFilePath = string.Empty;

        Cache.GeneratePrescanAODWaveformParam.WithFrequencyFlatness(Cache.PrescanFrequency);
        Cache.GeneratePrescanAODWaveformParam.Amplitude = Cache.DefaultAmplitude;
        Cache.GeneratePrescanAODWaveformParam.DirectoryPath = AODWaveformDirectoryPath;

        var (aodWaveformResult, exception) = AODWaveformGenerator.GeneratePrescanAODWaveform(Cache.GeneratePrescanAODWaveformParam.AdaptTo(), cancellationToken);
        if (aodWaveformResult.IsSuccess == false) throw GuardUtils.IsNotNullAndReturn(exception);

        Cache.PrescanAODWaveformProfiles = AODWaveformProfileFactory.CreatePrescanList(aodWaveformResult);
        Cache.PrescanAODWaveformResultFilePath = aodWaveformResult.FilePath;

        Logger.LogHtmlInformation("Prescan AOD Waveform", HtmlHeaderLevelEnum.Header6, new HtmlBullet(Cache.ToHtmlAnonymous()), HtmlLogUniqueId.LoggingHtml());
    }

    protected override void GenerateChangedAODWaveform(ChirpAODWaveformElectrodeOffsetItem item, CancellationToken cancellationToken)
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

        var (aodWaveformResult, exception) = AODWaveformGenerator.GenerateChirpAODWaveform(Cache.GenerateChirpAODWaveformParam.AdaptTo(), cancellationToken);
        if (aodWaveformResult.IsSuccess == false) throw GuardUtils.IsNotNullAndReturn(exception);

        item.ChirpAODWaveformProfiles = AODWaveformProfileFactory.CreateChirpList(aodWaveformResult);
        item.ChirpAODWaveformResultFilePath = aodWaveformResult.FilePath;

        Logger.LogHtmlInformation("Chirp AOD Waveform", HtmlHeaderLevelEnum.Header6, new HtmlQuote(Cache.ToHtmlAnonymous()), HtmlLogUniqueId.LoggingHtml());
    }

    protected override void SetAODWaveformProfiles(ChirpAODWaveformElectrodeOffsetItem item)
    {
        LaserViewModel.SetPrescanAODWaveProfiles(Cache.PrescanAODWaveformProfiles);
        LaserViewModel.SetChirpAODWaveProfiles(item.ChirpAODWaveformProfiles);
    }
}
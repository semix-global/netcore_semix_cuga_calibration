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

public sealed partial class PrescanAODWaveformElectrodeOffsetCache : AODWaveformElectrodeOffsetCache<PrescanAODWaveformElectrodeOffsetItem>
{
    [ObservableProperty]
    private double _chirpFrequency;

    [ObservableProperty]
    private string _chirpAODWaveformResultFilePath = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<ChirpAODWaveformProfile> _chirpAODWaveformProfiles = [];

    public override object ToHtmlAnonymous() => new
    {
        PrescanParam = new HtmlQuote(base.ToHtmlAnonymous()),
        ChirpAODWaveformResultFilePath,
        ChirpAODWaveformProfiles = new HtmlTable([.. ChirpAODWaveformProfiles.Select(t => t.ToHtmlAnonymous())])
    };
}

public sealed partial class PrescanAODWaveformElectrodeOffsetItem : AODWaveformElectrodeOffsetItem
{
    [ObservableProperty]
    private string _prescanAODWaveformResultFilePath = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<PrescanAODWaveformProfile> _prescanAODWaveformProfiles = [];

    public override object ToHtmlAnonymous() => new
    {
        Base = new HtmlQuote(base.ToHtmlAnonymous()),
        PrescanAODWaveformResultFilePath,
        PrescanAODWaveformProfiles = new HtmlTable([.. PrescanAODWaveformProfiles.Select(t => t.ToHtmlAnonymous())])
    };
}

[IOCAppService(ServiceType = typeof(PrescanAODWaveformElectrodeOffsetWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public class PrescanAODWaveformElectrodeOffsetWindowViewModel : AbstractAODWaveformElectrodeOffsetWindowViewModel<PrescanAODWaveformElectrodeOffsetCache, PrescanAODWaveformElectrodeOffsetItem>
{
    public override string Name => "Prescan AOD Waveform Electrode Offset";

    protected override void GenerateFixedAODWaveform(CancellationToken cancellationToken)
    {
        Cache.ChirpAODWaveformProfiles = [];
        Cache.ChirpAODWaveformResultFilePath = string.Empty;

        Cache.GenerateChirpAODWaveformParam.WithFrequencyFlatness(Cache.ChirpFrequency);
        Cache.GenerateChirpAODWaveformParam.Amplitude = Cache.DefaultAmplitude;
        Cache.GenerateChirpAODWaveformParam.DirectoryPath = AODWaveformDirectoryPath;

        var (aodWaveformResult, exception) = AODWaveformGenerator.GenerateChirpAODWaveform(Cache.GenerateChirpAODWaveformParam.AdaptTo(), cancellationToken);
        if (aodWaveformResult.IsSuccess == false) throw GuardUtils.IsNotNullAndReturn(exception);

        Cache.ChirpAODWaveformProfiles = AODWaveformProfileFactory.CreateChirpList(aodWaveformResult);
        Cache.ChirpAODWaveformResultFilePath = aodWaveformResult.FilePath;

        Logger.LogHtmlInformation("Chirp AOD Waveform", HtmlHeaderLevelEnum.Header6, new HtmlBullet(Cache.ToHtmlAnonymous()), HtmlLogUniqueId.LoggingHtml());
    }

    protected override void GenerateChangedAODWaveform(PrescanAODWaveformElectrodeOffsetItem item, CancellationToken cancellationToken)
    {
        Cache.GeneratePrescanAODWaveformParam.WithFrequencyFlatness(item.Frequency);
        Cache.GeneratePrescanAODWaveformParam.Amplitude = Cache.DefaultAmplitude;
        Cache.GeneratePrescanAODWaveformParam.DirectoryPath = AODWaveformDirectoryPath;
        Cache.GeneratePrescanAODWaveformParam.ElectrodeConfigurations =
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

        var (aodWaveformResult, exception) = AODWaveformGenerator.GeneratePrescanAODWaveform(Cache.GeneratePrescanAODWaveformParam.AdaptTo(), cancellationToken);
        if (aodWaveformResult.IsSuccess == false) throw GuardUtils.IsNotNullAndReturn(exception);

        item.PrescanAODWaveformProfiles = AODWaveformProfileFactory.CreatePrescanList(aodWaveformResult);
        item.PrescanAODWaveformResultFilePath = aodWaveformResult.FilePath;

        Logger.LogHtmlInformation("Prescan AOD Waveform", HtmlHeaderLevelEnum.Header6, new HtmlQuote(Cache.ToHtmlAnonymous()), HtmlLogUniqueId.LoggingHtml());
    }

    protected override void SetAODWaveformProfiles(PrescanAODWaveformElectrodeOffsetItem item)
    {
        LaserViewModel.SetPrescanAODWaveProfiles(item.PrescanAODWaveformProfiles);
        LaserViewModel.SetChirpAODWaveProfiles(Cache.ChirpAODWaveformProfiles);
    }
}
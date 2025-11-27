using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.AODWaveform.Generates;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public sealed partial class PrescanAODWaveformElectrodeOffsetCache : AODWaveformElectrodeOffsetCache<PrescanAODWaveformElectrodeOffsetItem, PrescanAODWaveformElectrodeOffsetResult>
{
    [ObservableProperty]
    private double _chirpFrequency;

    [ObservableProperty]
    private string _chirpAODWaveformResultFilePath = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<ChirpAODWaveformProfile> _chirpAODWaveformProfiles = [];
}

public sealed partial class PrescanAODWaveformElectrodeOffsetItem : AODWaveformElectrodeOffsetItem
{
    [ObservableProperty]
    private string _prescanAODWaveformResultFilePath = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<PrescanAODWaveformProfile> _prescanAODWaveformProfiles = [];
}

public sealed partial class PrescanAODWaveformElectrodeOffsetResult : AODWaveformElectrodeOffsetResult
{
    [ObservableProperty]
    private GeneratePrescanAODWaveformParam _generatePrescanAODWaveformParam = new();

    [ObservableProperty]
    private string _prescanAODWaveformResultFilePath = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<PrescanAODWaveformProfile> _prescanAODWaveformProfiles = [];
}

[IOCAppService(ServiceType = typeof(PrescanAODWaveformElectrodeOffsetWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public class PrescanAODWaveformElectrodeOffsetWindowViewModel : AbstractAODWaveformElectrodeOffsetWindowViewModel<PrescanAODWaveformElectrodeOffsetCache, PrescanAODWaveformElectrodeOffsetItem, PrescanAODWaveformElectrodeOffsetResult>
{
    public override string Name => "Prescan AOD Waveform Electrode Offset";

    protected override void GenerateFixedAODWaveform(CancellationToken cancellationToken)
    {
        Cache.ChirpAODWaveformProfiles = [];
        Cache.ChirpAODWaveformResultFilePath = string.Empty;

        Cache.GenerateChirpAODWaveformParam.ProductivityInformation = Cache.ProductivityInformation;
        Cache.GenerateChirpAODWaveformParam.WithFrequencyFlatness(Cache.ChirpFrequency);
        Cache.GenerateChirpAODWaveformParam.DirectoryPath = AODWaveformDirectoryPath;

        var (aodWaveformResult, exception) = AODWaveformGenerator.GenerateChirpAODWaveform(Cache.GenerateChirpAODWaveformParam.AdaptTo(), cancellationToken);
        if (aodWaveformResult.IsSuccess == false) ThrowHelper.ThrowInvalidOperationException(string.Empty, GuardUtils.IsNotNullAndReturn(exception));

        Cache.ChirpAODWaveformProfiles = AODWaveformProfileFactory.CreateChirpList(aodWaveformResult);
        Cache.ChirpAODWaveformResultFilePath = aodWaveformResult.FilePath;

        Logger.LogHtmlInformation("Chirp AOD Waveform", HtmlHeaderLevelEnum.Header6, new HtmlBullet(new
        {
            GenerateChirpAODWaveformParam = new HtmlQuote(Cache.GenerateChirpAODWaveformParam.ToFlatnessHtmlAnonymous()),
            Cache.ChirpAODWaveformResultFilePath,
            ChirpAODWaveformProfiles = new HtmlTable([.. Cache.ChirpAODWaveformProfiles.Select(t => t.ToFlatnessHtmlAnonymous())])
        }), HtmlLogUniqueId.LoggingHtml());
    }

    protected override void GenerateChangedAODWaveform(PrescanAODWaveformElectrodeOffsetItem item, CancellationToken cancellationToken)
    {
        Cache.GeneratePrescanAODWaveformParam.ProductivityInformation = Cache.ProductivityInformation;
        Cache.GeneratePrescanAODWaveformParam.WithFrequencyFlatness(item.Frequency);
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
        if (aodWaveformResult.IsSuccess == false) ThrowHelper.ThrowInvalidOperationException(string.Empty, GuardUtils.IsNotNullAndReturn(exception));

        item.PrescanAODWaveformProfiles = AODWaveformProfileFactory.CreatePrescanList(aodWaveformResult);
        item.PrescanAODWaveformResultFilePath = aodWaveformResult.FilePath;

        Logger.LogHtmlInformation("Prescan AOD Waveform", HtmlHeaderLevelEnum.Header6, new HtmlBullet(new
        {
            GeneratePrescanAODWaveformParam = new HtmlQuote(Cache.GeneratePrescanAODWaveformParam.ToFlatnessHtmlAnonymous()),
            item.PrescanAODWaveformResultFilePath,
            PrescanAODWaveformProfiles = new HtmlTable([.. item.PrescanAODWaveformProfiles.Select(t => t.ToFlatnessHtmlAnonymous())])
        }), HtmlLogUniqueId.LoggingHtml());
    }

    protected override void SetAODWaveformProfiles(PrescanAODWaveformElectrodeOffsetItem item)
    {
        LaserViewModel.SetPrescanAODWaveProfiles(item.PrescanAODWaveformProfiles, OpticsIncidentModeEnum.OI);
        LaserViewModel.SetChirpAODWaveProfiles(Cache.ChirpAODWaveformProfiles, OpticsIncidentModeEnum.OI);
    }

    protected override void GenerateResultAODWaveform(CancellationToken cancellationToken)
    {
        Guard.IsTrue(Cache.Results.DistinctBy(t => t.GeneratePrescanAODWaveformParam.ProductivityInformation).Count() == Cache.Results.Count, "The Productivity Information of the results must be the same.");

        foreach (var result in Cache.Results)
        {
            cancellationToken.ThrowIfCancellationRequested();

            result.GeneratePrescanAODWaveformParam.DirectoryPath = ResultAODWaveformDirectoryPath;
            result.GeneratePrescanAODWaveformParam.ElectrodeConfigurations = Cache.ElectrodeConfigurationResults;

            var (aodWaveformResult, exception) = AODWaveformGenerator.GeneratePrescanAODWaveform(result.GeneratePrescanAODWaveformParam.AdaptTo(), cancellationToken);
            if (aodWaveformResult.IsSuccess == false) ThrowHelper.ThrowInvalidOperationException(string.Empty, GuardUtils.IsNotNullAndReturn(exception));

            result.PrescanAODWaveformProfiles = AODWaveformProfileFactory.CreatePrescanList(aodWaveformResult);
            result.PrescanAODWaveformResultFilePath = aodWaveformResult.FilePath;

            Logger.LogHtmlInformation($"{result.GeneratePrescanAODWaveformParam.ProductivityInformation}", HtmlHeaderLevelEnum.Header6, new HtmlBullet(new
            {
                GeneratePrescanAODWaveformParam = new HtmlQuote(result.GeneratePrescanAODWaveformParam.ToHtmlAnonymous()),
                result.PrescanAODWaveformResultFilePath,
                PrescanAODWaveformProfiles = new HtmlTable([.. result.PrescanAODWaveformProfiles.Select(t => t.ToHtmlAnonymous())])
            }), HtmlLogUniqueId.LoggingHtml());
        }
    }

    protected override void SetResultAODWaveformProfiles(PrescanAODWaveformElectrodeOffsetResult result, CancellationToken cancellationToken)
    {
        LaserViewModel.SetPrescanAODWaveProfiles(result.PrescanAODWaveformProfiles, OpticsIncidentModeEnum.NI);
    }

    protected override void SetResultAODWaveformConfig(PrescanAODWaveformElectrodeOffsetResult result, CancellationToken cancellationToken)
    {
        ConfigViewModel.SetPrescanAODWaveProfiles(result.GeneratePrescanAODWaveformParam.ProductivityInformation, result.PrescanAODWaveformResultFilePath);

        Logger.LogHtmlInformation($"{result.GeneratePrescanAODWaveformParam.ProductivityInformation}", HtmlHeaderLevelEnum.Header6, new HtmlBullet(new
        {
            GeneratePrescanAODWaveformParam = new HtmlQuote(result.GeneratePrescanAODWaveformParam.ToHtmlAnonymous()),
            result.PrescanAODWaveformResultFilePath,
            PrescanAODWaveformProfiles = new HtmlTable([.. result.PrescanAODWaveformProfiles.Select(t => t.ToHtmlAnonymous())])
        }), HtmlLogUniqueId.LoggingHtml());
    }
}
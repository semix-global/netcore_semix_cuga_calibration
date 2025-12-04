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

public sealed partial class ChirpAODWaveformElectrodeOffsetCache : AODWaveformElectrodeOffsetCache<ChirpAODWaveformElectrodeOffsetItem, ChirpAODWaveformElectrodeOffsetResult>
{
    [ObservableProperty]
    private double _prescanFrequency;

    [ObservableProperty]
    private string _prescanAODWaveformResultFilePath = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<PrescanAODWaveformProfile> _prescanAODWaveformProfiles = [];
}

public sealed partial class ChirpAODWaveformElectrodeOffsetItem : AODWaveformElectrodeOffsetItem
{
    [ObservableProperty]
    private string _chirpAODWaveformResultFilePath = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<ChirpAODWaveformProfile> _chirpAODWaveformProfiles = [];
}

public sealed partial class ChirpAODWaveformElectrodeOffsetResult : AODWaveformElectrodeOffsetResult
{
    [ObservableProperty]
    private GenerateChirpAODWaveformParam _generateChirpAODWaveformParam = new();

    [ObservableProperty]
    private string _chirpAODWaveformResultFilePath = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<ChirpAODWaveformProfile> _chirpAODWaveformProfiles = [];
}

[IOCAppService(ServiceType = typeof(ChirpAODWaveformElectrodeOffsetWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public class ChirpAODWaveformElectrodeOffsetWindowViewModel : AbstractAODWaveformElectrodeOffsetWindowViewModel<ChirpAODWaveformElectrodeOffsetCache, ChirpAODWaveformElectrodeOffsetItem, ChirpAODWaveformElectrodeOffsetResult>
{
    public override string Name => "Chirp AOD Waveform Electrode Offset";

    protected override void GenerateFixedAODWaveform(CancellationToken cancellationToken)
    {
        Cache.PrescanAODWaveformProfiles = [];
        Cache.PrescanAODWaveformResultFilePath = string.Empty;

        Cache.GeneratePrescanAODWaveformParam.ProductivityInformation = Cache.ProductivityInformation;
        Cache.GeneratePrescanAODWaveformParam.WithFrequencyFlatness(Cache.PrescanFrequency);
        Cache.GeneratePrescanAODWaveformParam.DirectoryPath = AODWaveformDirectoryPath;

        var (aodWaveformResult, exception) = AODWaveformGenerator.GeneratePrescanAODWaveform(Cache.GeneratePrescanAODWaveformParam.AdaptTo(), cancellationToken);
        if (aodWaveformResult.IsSuccess == false) ThrowHelper.ThrowInvalidOperationException(string.Empty, GuardUtils.IsNotNullAndReturn(exception));

        Cache.PrescanAODWaveformProfiles = AODWaveformProfileFactory.CreatePrescanList(aodWaveformResult);
        Cache.PrescanAODWaveformResultFilePath = aodWaveformResult.FilePath;

        Logger.LogHtmlInformation("Prescan AOD Waveform", HtmlHeaderLevelEnum.Header6, new HtmlBullet(new
        {
            GeneratePrescanAODWaveformParam = new HtmlQuote(Cache.GeneratePrescanAODWaveformParam.ToFlatnessHtmlAnonymous()),
            Cache.PrescanAODWaveformResultFilePath,
            PrescanAODWaveformProfiles = new HtmlTable([.. Cache.PrescanAODWaveformProfiles.Select(t => t.ToFlatnessHtmlAnonymous())])
        }), HtmlLogUniqueId.LoggingHtml());
    }

    protected override void GenerateChangedAODWaveform(ChirpAODWaveformElectrodeOffsetItem item, CancellationToken cancellationToken)
    {
        Cache.GenerateChirpAODWaveformParam.ProductivityInformation = Cache.ProductivityInformation;
        Cache.GenerateChirpAODWaveformParam.WithFrequencyFlatness(item.Frequency);
        Cache.GenerateChirpAODWaveformParam.DirectoryPath = AODWaveformDirectoryPath;
        Cache.GenerateChirpAODWaveformParam.ElectrodeConfigurations = item.ElectrodeConfigurations;

        var (aodWaveformResult, exception) = AODWaveformGenerator.GenerateChirpAODWaveform(Cache.GenerateChirpAODWaveformParam.AdaptTo(), cancellationToken);
        if (aodWaveformResult.IsSuccess == false) ThrowHelper.ThrowInvalidOperationException(string.Empty, GuardUtils.IsNotNullAndReturn(exception));

        item.ChirpAODWaveformProfiles = AODWaveformProfileFactory.CreateChirpList(aodWaveformResult);
        item.ChirpAODWaveformResultFilePath = aodWaveformResult.FilePath;

        Logger.LogHtmlInformation("Chirp AOD Waveform", HtmlHeaderLevelEnum.Header6, new HtmlBullet(new
        {
            GenerateChirpAODWaveformParam = new HtmlQuote(Cache.GenerateChirpAODWaveformParam.ToFlatnessHtmlAnonymous()),
            item.ChirpAODWaveformResultFilePath,
            ChirpAODWaveformProfiles = new HtmlTable([.. item.ChirpAODWaveformProfiles.Select(t => t.ToFlatnessHtmlAnonymous())])
        }), HtmlLogUniqueId.LoggingHtml());
    }

    protected override void SetAODWaveformProfiles(ChirpAODWaveformElectrodeOffsetItem item)
    {
        LaserViewModel.SetPrescanAODWaveProfiles(OpticsIlluminationModeEnum.OI, Cache.PrescanAODWaveformProfiles);
        LaserViewModel.SetChirpAODWaveProfiles(OpticsIlluminationModeEnum.OI, item.ChirpAODWaveformProfiles);
    }

    protected override void GenerateResultAODWaveform(CancellationToken cancellationToken)
    {
        Guard.IsTrue(Cache.Results.DistinctBy(t => t.GenerateChirpAODWaveformParam.ProductivityInformation).Count() == Cache.Results.Count, "The Productivity Information of the results must be the same.");

        foreach (var result in Cache.Results)
        {
            cancellationToken.ThrowIfCancellationRequested();

            result.GenerateChirpAODWaveformParam.DirectoryPath = ResultAODWaveformDirectoryPath;
            result.GenerateChirpAODWaveformParam.ElectrodeConfigurations = Cache.ElectrodeConfigurationResults;

            var (aodWaveformResult, exception) = AODWaveformGenerator.GenerateChirpAODWaveform(result.GenerateChirpAODWaveformParam.AdaptTo(), cancellationToken);
            if (aodWaveformResult.IsSuccess == false) ThrowHelper.ThrowInvalidOperationException(string.Empty, GuardUtils.IsNotNullAndReturn(exception));

            result.ChirpAODWaveformProfiles = AODWaveformProfileFactory.CreateChirpList(aodWaveformResult);
            result.ChirpAODWaveformResultFilePath = aodWaveformResult.FilePath;

            Logger.LogHtmlInformation($"{result.GenerateChirpAODWaveformParam.ProductivityInformation}", HtmlHeaderLevelEnum.Header6, new HtmlBullet(new
            {
                GenerateChirpAODWaveformParam = new HtmlQuote(result.GenerateChirpAODWaveformParam.ToHtmlAnonymous()),
                result.ChirpAODWaveformResultFilePath,
                ChirpAODWaveformProfiles = new HtmlTable([.. result.ChirpAODWaveformProfiles.Select(t => t.ToHtmlAnonymous())])
            }), HtmlLogUniqueId.LoggingHtml());
        }
    }

    protected override void SetResultAODWaveformProfiles(ChirpAODWaveformElectrodeOffsetResult result, CancellationToken cancellationToken)
    {
        LaserViewModel.SetChirpAODWaveProfiles(OpticsIlluminationModeEnum.NI, result.ChirpAODWaveformProfiles);
    }

    protected override void SetResultAODWaveformConfig(ChirpAODWaveformElectrodeOffsetResult result, CancellationToken cancellationToken)
    {
        ConfigViewModel.SetChirpAODWaveProfiles(OpticsIlluminationModeEnum.NI, result.GenerateChirpAODWaveformParam.ProductivityInformation, result.ChirpAODWaveformResultFilePath);

        Logger.LogHtmlInformation($"{result.GenerateChirpAODWaveformParam.ProductivityInformation}", HtmlHeaderLevelEnum.Header6, new HtmlBullet(new
        {
            GenerateChirpAODWaveformParam = new HtmlQuote(result.GenerateChirpAODWaveformParam.ToHtmlAnonymous()),
            result.ChirpAODWaveformResultFilePath,
            ChirpAODWaveformProfiles = new HtmlTable([.. result.ChirpAODWaveformProfiles.Select(t => t.ToHtmlAnonymous())])
        }), HtmlLogUniqueId.LoggingHtml());
    }
}
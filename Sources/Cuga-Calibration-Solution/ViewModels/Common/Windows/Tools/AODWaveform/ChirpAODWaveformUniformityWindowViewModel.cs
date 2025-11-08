using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.AODWaveform;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public sealed partial class ChirpAODWaveformUniformityCache : AODWaveformUniformityCache<ChirpAODWaveformUniformityItem>
{
    [ObservableProperty]
    private double _prescanFrequency;

    [ObservableProperty]
    private string _prescanAODWaveformResultFilePath = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<PrescanAODWaveformProfile> _prescanAODWaveformProfiles = [];
}

public sealed partial class ChirpAODWaveformUniformityItem : AODWaveformUniformityItem
{
    [ObservableProperty]
    private string _chirpAODWaveformResultFilePath = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<ChirpAODWaveformProfile> _chirpAODWaveformProfiles = [];
}

[IOCAppService(ServiceType = typeof(ChirpAODWaveformUniformityWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public class ChirpAODWaveformUniformityWindowViewModel : AbstractAODWaveformUniformityWindowViewModel<ChirpAODWaveformUniformityCache, ChirpAODWaveformUniformityItem>
{
    public override string Name => "Chirp AOD Waveform Uniformity";

    protected override void GenerateFixedAODWaveform(CancellationToken cancellationToken)
    {
        Cache.PrescanAODWaveformProfiles = [];
        Cache.PrescanAODWaveformResultFilePath = string.Empty;

        Cache.GeneratePrescanAODWaveformParam.OpticsMagTypeEnum = Cache.OpticsMagTypeEnum;
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

    protected override void GenerateChangedAODWaveform(ChirpAODWaveformUniformityItem item, CancellationToken cancellationToken)
    {
        Cache.GenerateChirpAODWaveformParam.OpticsMagTypeEnum = Cache.OpticsMagTypeEnum;
        Cache.GenerateChirpAODWaveformParam.WithFrequencyFlatness(item.Frequency);
        Cache.GenerateChirpAODWaveformParam.DirectoryPath = AODWaveformDirectoryPath;

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

    protected override void SetAODWaveformProfiles(ChirpAODWaveformUniformityItem item)
    {
        LaserViewModel.SetPrescanAODWaveProfiles(Cache.PrescanAODWaveformProfiles);
        LaserViewModel.SetChirpAODWaveProfiles(item.ChirpAODWaveformProfiles);
    }
}
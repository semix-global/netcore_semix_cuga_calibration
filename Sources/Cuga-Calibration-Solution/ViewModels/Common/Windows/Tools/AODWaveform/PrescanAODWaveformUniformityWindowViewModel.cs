using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Models.Common.AODWaveform;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public sealed partial class PrescanAODWaveformUniformityCache : AODWaveformUniformityCache<PrescanAODWaveformUniformityItem>
{
    [ObservableProperty]
    private double _chirpFrequency;

    [ObservableProperty]
    private string _chirpAODWaveformResultFilePath = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<ChirpAODWaveformProfile> _chirpAODWaveformProfiles = [];
}

public sealed partial class PrescanAODWaveformUniformityItem : AODWaveformUniformityItem
{
    [ObservableProperty]
    private string _prescanAODWaveformResultFilePath = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<PrescanAODWaveformProfile> _prescanAODWaveformProfiles = [];
}

[IOCAppService(ServiceType = typeof(PrescanAODWaveformUniformityWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public class PrescanAODWaveformUniformityWindowViewModel : AbstractAODWaveformUniformityWindowViewModel<PrescanAODWaveformUniformityCache, PrescanAODWaveformUniformityItem>
{
    public override string Name => "Prescan AOD Waveform Uniformity";

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

    protected override void GenerateChangedAODWaveform(PrescanAODWaveformUniformityItem item, CancellationToken cancellationToken)
    {
        Cache.GeneratePrescanAODWaveformParam.ProductivityInformation = Cache.ProductivityInformation;
        Cache.GeneratePrescanAODWaveformParam.WithFrequencyFlatness(item.Frequency);
        Cache.GeneratePrescanAODWaveformParam.DirectoryPath = AODWaveformDirectoryPath;

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

    protected override void SetAODWaveformProfiles(PrescanAODWaveformUniformityItem item)
    {
        LaserViewModel.SetPrescanAODWaveProfiles(OpticsIlluminationModeEnum.OI, item.PrescanAODWaveformProfiles);
        LaserViewModel.SetChirpAODWaveProfiles(OpticsIlluminationModeEnum.OI, Cache.ChirpAODWaveformProfiles);
    }
}
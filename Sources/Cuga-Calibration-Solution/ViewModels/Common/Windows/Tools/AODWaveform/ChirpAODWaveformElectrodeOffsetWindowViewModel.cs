using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.AODWaveform.Generates;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Common.Pattern;
using Core.Utilities;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Nlog.Entities.HtmlElements;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public sealed partial class ChirpAODWaveformElectrodeOffsetCache : AODWaveformElectrodeOffsetCache<GenerateChirpAODWaveformParam, ChirpAODWaveformProfile, ChirpAODWaveformElectrodeOffsetItem>
{
    [ObservableProperty]
    private LaserLightInformation _laserLightInformation = LaserLightInformation.Default;

    public override object ToHtmlAnonymous() => new
    {
        LaserLightInformation,
        Base = new HtmlBullet(base.ToHtmlAnonymous())
    };
}

public sealed class ChirpAODWaveformElectrodeOffsetItem : AODWaveformElectrodeOffsetItem<ChirpAODWaveformProfile>;

[IOCAppService(ServiceType = typeof(ChirpAODWaveformElectrodeOffsetWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public class ChirpAODWaveformElectrodeOffsetWindowViewModel(ApplicationCookie applicationCookie) : AbstractAODWaveformElectrodeOffsetWindowViewModel<GenerateChirpAODWaveformParam, ChirpAODWaveformProfile, ChirpAODWaveformElectrodeOffsetItem, ChirpAODWaveformElectrodeOffsetCache>
{
    public IReadOnlyList<LaserLightInformation> LaserLightInformationList => applicationCookie.LaserLightInformationList;

    protected override string AODWaveformName => "Chirp";

    protected override (bool IsSuccess, IReadOnlyList<ChirpAODWaveformProfile> Result, string ResultFilePath, Exception? Exception) GenerateAODWaveform(GenerateChirpAODWaveformParam param, CancellationToken cancellationToken)
    {
        var (aodWaveformResult, exception) = AODWaveformGenerator.GenerateChirpAODWaveform(param.AdaptTo(), cancellationToken);

        return (aodWaveformResult.IsSuccess, AODWaveformProfileFactory.CreateChirpList(aodWaveformResult), aodWaveformResult.FilePath, exception);
    }

    protected override void SetAODWaveProfiles(GenerateChirpAODWaveformParam param, IReadOnlyList<ChirpAODWaveformProfile> profiles)
    {
        var ps = AODWaveformProfileFactory.CreatePrescan(OpticsAODElectrodeEnum.Electrode1, "D:\\UserS\\Administrator\\桌面\\test\\Electrode1\\prescan_Low$2949$0$600$02$0$0$.txt");
        LaserViewModel.SetPrescanAODWaveProfiles([ps]);
        LaserViewModel.SetChirpAODWaveProfiles(profiles);
    }
}
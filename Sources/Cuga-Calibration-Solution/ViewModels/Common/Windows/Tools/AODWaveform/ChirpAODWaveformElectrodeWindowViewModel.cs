using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.AODWaveform.Generates;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Common.Pattern;
using Core.Utilities;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Nlog.Entities.HtmlElements;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public sealed partial class ChirpAODWaveformElectrodeCache : AODWaveformElectrodeCache<GenerateChirpAODWaveformParam, ChirpAODWaveformProfile, ChirpAODWaveformElectrodeItem>
{
    [ObservableProperty]
    private LaserLightInformation _laserLightInformation = LaserLightInformation.Default;

    public override object ToHtmlAnonymous() => new
    {
        LaserLightInformation,
        Base = new HtmlBullet(base.ToHtmlAnonymous())
    };
}

public sealed class ChirpAODWaveformElectrodeItem : AODWaveformElectrodeItem<ChirpAODWaveformProfile>;

[IOCAppService(ServiceType = typeof(ChirpAODWaveformElectrodeWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public class ChirpAODWaveformElectrodeWindowViewModel(ApplicationCookie applicationCookie) : AbstractAODWaveformElectrodeWindowViewModel<GenerateChirpAODWaveformParam, ChirpAODWaveformProfile, ChirpAODWaveformElectrodeItem, ChirpAODWaveformElectrodeCache>
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
        LaserViewModel.SetPrescanAODWaveProfileByCoefficient(param.OpticsMagTypeEnum, Cache.LaserLightInformation.Coefficient);
        LaserViewModel.SetChirpAODWaveProfiles(profiles);
    }
}
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.AODWaveform.Generates;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Common.Pattern;
using Core.Utilities;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public sealed partial class ChirpAODWaveformUniformityCache : AODWaveformUniformityCache<GenerateChirpAODWaveformParam>
{
    [ObservableProperty]
    private LaserLightInformation _laserLightInformation = LaserLightInformation.Default;
}

public sealed class ChirpAODWaveformUniformityItem : AODWaveformUniformityItem<ChirpAODWaveformProfile>;

[IOCAppService(ServiceType = typeof(ChirpAODWaveformUniformityWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public class ChirpAODWaveformUniformityWindowViewModel(ApplicationCookie applicationCookie) : AbstractAODWaveformUniformityWindowViewModel<ChirpAODWaveformUniformityCache, ChirpAODWaveformUniformityItem, GenerateChirpAODWaveformParam, ChirpAODWaveformProfile>
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
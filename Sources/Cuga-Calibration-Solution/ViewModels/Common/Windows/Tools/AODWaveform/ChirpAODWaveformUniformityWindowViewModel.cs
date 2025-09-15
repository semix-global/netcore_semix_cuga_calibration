using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.AODWaveform.Generates;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Common.Pattern;
using Core.Utilities;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

[IOCAppService(ServiceType = typeof(ChirpAODWaveformUniformityWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public partial class ChirpAODWaveformUniformityWindowViewModel(ApplicationCookie applicationCookie) : AbstractAODWaveformUniformityWindowViewModel<GenerateChirpAODWaveformParam, ChirpAODWaveformProfile>
{
    public IReadOnlyList<LaserLightInformation> LaserLightInformationList => applicationCookie.LaserLightInformationList;

    [ObservableProperty]
    private LaserLightInformation _laserLightInformation = LaserLightInformation.Default;

    protected override string AODWaveformName => "Chirp";

    protected override (bool IsSuccess, IReadOnlyList<ChirpAODWaveformProfile> Result, string ResultFilePath, Exception? Exception) GenerateAODWaveform(GenerateChirpAODWaveformParam param, CancellationToken cancellationToken)
    {
        var (aodWaveformResult, exception) = AODWaveformGenerator.GenerateChirpAODWaveform(param.AdaptTo(), cancellationToken);

        return (aodWaveformResult.IsSuccess, AODWaveformProfileFactory.CreateChirpList(aodWaveformResult), aodWaveformResult.FilePath, exception);
    }

    protected override void SetAODWaveProfiles(GenerateChirpAODWaveformParam param, IReadOnlyList<ChirpAODWaveformProfile> profiles)
    {
        LaserViewModel.SetPrescanAODWaveProfileByCoefficient(param.OpticsMagTypeEnum, LaserLightInformation.Coefficient);
        LaserViewModel.SetChirpAODWaveProfiles(profiles);
    }
}
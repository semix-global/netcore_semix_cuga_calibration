using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

[IOCAppService(ServiceType = typeof(V0AODWaveformElectrodeOffsetFrequencyPeriodConfirmResultWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Transient)]
public sealed partial class V0AODWaveformElectrodeOffsetFrequencyPeriodConfirmResultWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial double OffsetFrequencyPeriodCoefficient { get; set; }

    [RelayCommand]
    private void Close()
    {
        CloseView(true);
    }
}
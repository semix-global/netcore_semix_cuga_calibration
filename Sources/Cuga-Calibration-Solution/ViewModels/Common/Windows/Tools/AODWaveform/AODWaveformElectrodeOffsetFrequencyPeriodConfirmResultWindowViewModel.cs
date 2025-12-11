using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

[IOCAppService(ServiceType = typeof(AODWaveformElectrodeOffsetFrequencyPeriodConfirmResultWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Transient)]
public sealed partial class AODWaveformElectrodeOffsetFrequencyPeriodConfirmResultWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private double _offsetFrequencyPeriodCoefficient;

    [RelayCommand]
    private void Close()
    {
        CloseView(true);
    }
}
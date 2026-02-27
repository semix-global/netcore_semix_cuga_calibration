using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Core.Models.Events;
using CugaCalibration.ViewModels.Common.Windows.Diagnosis.AdsDiagonosis;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.WPF.MVVM;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using System.Windows.Controls;

namespace CugaCalibration.ViewModels.Common.Windows.Diagnosis;

[IOCAppService(ServiceType = typeof(AdsDiagnosisWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class AdsDiagnosisWindowViewModel : ViewModelBase, IRecipient<ValueChangedMessage<ToggleCalibrateEvent>>
{
    private readonly IMessenger _messenger;

    [ObservableProperty]
    private bool _isEnableWindow = true;

    [ObservableProperty]
    private AdsDiagnosisViewModelBase? _activeItem;

    [ObservableProperty]
    private AdsGainsDiagnosisViewModel? _adsGainsDiagnosisViewModel;

    public AdsDiagnosisWindowViewModel()
    {
        _messenger = HostApplication.GetRequiredService<IMessenger>();
        ActiveItem = AdsGainsDiagnosisViewModel = HostApplication.GetRequiredService<AdsGainsDiagnosisViewModel>();
        _messenger.RegisterAll(this);
    }

    [RelayCommand]
    private void TabItemChanged(object obj)
    {
        if (obj is not TabItem tabItem)
            return;
        string name = tabItem.Header.ToString();
        switch (name)
        {
            case "X Gains":
                AdsGainsDiagnosisViewModel!.IsX = true;
                ActiveItem = AdsGainsDiagnosisViewModel;
                break;

            case "Y Gains":
                AdsGainsDiagnosisViewModel!.IsX = false;
                ActiveItem = AdsGainsDiagnosisViewModel;
                break;

            default:
                break;
        }
    }

    [RelayCommand(CanExecute = nameof(IsEnableWindow))]
    private void Close()
    {
        _messenger.UnregisterAll(this);
        CloseView(true);
    }

    [RelayCommand(CanExecute = nameof(IsEnableWindow))]
    private void ImportConfig()
    {
        ActiveItem?.ImportConfig();
    }

    [RelayCommand(CanExecute = nameof(IsEnableWindow))]
    private Task SaveAsync()
    {
        return ActiveItem?.SaveAsync() ?? Task.CompletedTask;
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task ActionAsync(CancellationToken cancellationToken)
    {
        return ActiveItem?.ActionAsync(cancellationToken) ?? Task.CompletedTask;
    }

    public void Receive(ValueChangedMessage<ToggleCalibrateEvent> message)
    {
        if (message.Value.IsWindowEnable.HasValue)
            IsEnableWindow = message.Value.IsWindowEnable.Value;
    }
}
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Core.Models.Events;
using CugaCalibration.ViewModels.Common.Windows.Diagnosis.RtfcDiagnosis;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.IOC.Providers;
using Net.Utilities.WPF.MVVM;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using System.Windows.Controls;

namespace CugaCalibration.ViewModels.Common.Windows.Diagnosis;

[IOCAppService(ServiceType = typeof(RtfcDiagnosisWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class RtfcDiagnosisWindowViewModel : ViewModelBase, IRecipient<ValueChangedMessage<ToggleCalibrateEvent>>
{
    private readonly IMessenger _messenger;
    public readonly ISynchronizationContextProvider _synchronizationContextProvider;

    [ObservableProperty]
    private bool _isEnableWindow = true;

    [ObservableProperty]
    private RtfcDiagnosisViewModelBase? _activeItem;

    [ObservableProperty]
    private AfFocusDiagnosisViewModel? _afFocusDiagnosisViewModel;

    public RtfcDiagnosisWindowViewModel()
    {
        _messenger = HostApplication.GetRequiredService<IMessenger>();
        ActiveItem = AfFocusDiagnosisViewModel = HostApplication.GetRequiredService<AfFocusDiagnosisViewModel>();
        _synchronizationContextProvider = HostApplication.GetRequiredService<ISynchronizationContextProvider>();
        _messenger.RegisterAll(this);
    }

    [RelayCommand]
    private void TabItemChanged(object obj)
    {
        if (obj is not TabItem tabItem)
            return;
        string name = tabItem.Header.ToString()!;
        switch (name)
        {
            case "Af Focus":
                ActiveItem = AfFocusDiagnosisViewModel;
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
    private Task SaveAsync()
    {
        return ActiveItem?.SaveAsync() ?? Task.CompletedTask;
    }

    [RelayCommand(IncludeCancelCommand = true, CanExecute = nameof(IsEnableWindow))]
    private Task ActionAsync(CancellationToken cancellationToken)
    {
        return ActiveItem?.ActionAsync(cancellationToken) ?? Task.CompletedTask;
    }

    public void Receive(ValueChangedMessage<ToggleCalibrateEvent> message)
    {
        if (message.Value.IsWindowEnable.HasValue)
        {
            IsEnableWindow = message.Value.IsWindowEnable.Value;
            _synchronizationContextProvider.Send(() => OnPropertyChanged(nameof(IsEnableWindow)));
        }
    }
}
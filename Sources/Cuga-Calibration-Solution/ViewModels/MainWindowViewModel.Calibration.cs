using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Core.Models.Events;
using Core.Models.Models.Common.Cookies;
using CugaCalibration.ViewModels.Common.Windows.View;
using Microsoft.Extensions.Logging;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM;

namespace CugaCalibration.ViewModels;

public sealed partial class MainWindowViewModel : IRecipient<ValueChangedMessage<ToggleCalibrateEvent>>
{
    [ObservableProperty]
    public partial CalibrationViewModelBase? ActiveItem { get; set; }

    [ObservableProperty]
    public partial bool IsCalibrateEnable { get; set; }

    [ObservableProperty]
    public partial bool IsReviewEnable { get; set; }

    [ObservableProperty]
    public partial bool IsCancelEnable { get; set; }

    [ObservableProperty]
    public partial bool IsPreviousEnable { get; set; }

    [ObservableProperty]
    public partial bool IsNextEnable { get; set; }

    [RelayCommand]
    private async Task CalibrateAsync() => await (ActiveItem?.CalibrateAsync() ?? Task.CompletedTask).ConfigureAwait(false);

    [RelayCommand]
    private async Task ReviewAsync() => await (ActiveItem?.ReviewAsync() ?? Task.CompletedTask).ConfigureAwait(false);

    [RelayCommand]
    private async Task CancelAsync()
    {
        await (ActiveItem?.CancelAsync() ?? Task.CompletedTask).ConfigureAwait(false);

        if (ActiveItem is not null) ActiveItem = null;
    }

    [RelayCommand]
    private async Task PreviousAsync() => await (ActiveItem?.PreviousAsync() ?? Task.CompletedTask).ConfigureAwait(false);

    [RelayCommand]
    private async Task NextAsync() => await (ActiveItem?.NextAsync() ?? Task.CompletedTask).ConfigureAwait(false);

    [RelayCommand]
    private async Task ExportAsync() => await (ActiveItem?.ExportAsync() ?? Task.CompletedTask).ConfigureAwait(false);

    [RelayCommand]
    private async Task ImportAsync() => await (ActiveItem?.ImportAsync() ?? Task.CompletedTask).ConfigureAwait(false);

    [RelayCommand]
    private async Task OpenCalibrationAsync(string viewModel)
    {
        await Task.Run(() =>
        {
            try
            {
                if (ActiveItem is not null)
                {
                    if (ActiveItem.GetType().FullName != viewModel) _dialogWindowProvider.ShowDialog("Calibration, cannot be switched", DialogButtonsEnum.OK, DialogIconEnum.Warning);

                    return;
                }

                ActiveItem = HostApplication.GetRequiredService<CalibrationViewModelBase>(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "{@Name}: Load Calibration View({@ViewModel}) Failed", nameof(MainWindowViewModel), viewModel);
            }
        });
    }

    [RelayCommand]
    private async Task ShowCalibrationMenuAsync(CalibrationMenu calibrationMenu)
    {
        await Task.Run(() =>
        {
            var popupWindowViewModelFactory = HostApplication.GetRequiredService<Func<Type, CalibrationMenuWindowViewModel>>();
            var popupWindowViewModel = popupWindowViewModelFactory(calibrationMenu.Entry.ViewModelType);

            popupWindowViewModel.CalibrationMenu = calibrationMenu;

            if (popupWindowViewModel.Show() == false) _windowManagerService.ShowWindow(popupWindowViewModel);
        });
    }

    public void Receive(ValueChangedMessage<ToggleCalibrateEvent> message)
    {
        if (message.Value.IsCalibrateEnable.HasValue) IsCalibrateEnable = message.Value.IsCalibrateEnable.Value;

        if (message.Value.IsReviewEnable.HasValue) IsReviewEnable = message.Value.IsReviewEnable.Value;

        if (message.Value.IsCancelEnable.HasValue) IsCancelEnable = message.Value.IsCancelEnable.Value;

        if (message.Value.IsPreviousEnable.HasValue) IsPreviousEnable = message.Value.IsPreviousEnable.Value;

        if (message.Value.IsNextEnable.HasValue) IsNextEnable = message.Value.IsNextEnable.Value;
    }
}
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Setting;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.WPF.MVVM;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;

namespace CugaCalibration.ViewModels.Common.Windows.Management;

[IOCAppService(ServiceType = typeof(SystemManagementViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class SystemManagementViewModel(
    ILogger<MainWindowViewModel> logger,
    IDialogWindowProvider dialogWindowProvider,
    ApplicationCookie applicationCookie,
    CalibrationSetting calibrationSetting) : ViewModelBase
{
    private readonly IDialogWindowProvider _dialogWindowProvider = dialogWindowProvider;

    [ObservableProperty]
    private ApplicationCookie _applicationCookie = applicationCookie;

    [ObservableProperty]
    private CalibrationSetting _calibrationSetting = calibrationSetting;

    [ObservableProperty]
    private ManagementViewModelBase? _activeItem;

    [RelayCommand]
    private void OpenManagementMenu(string viewModel)
    {
        try
        {
            if (ActiveItem?.GetType().FullName == viewModel) return;
            // 获取menu中选择的校准大类的服务
            var abstractCalibrationViewModel = HostApplication.GetRequiredService<ManagementViewModelBase>(viewModel);
            if (abstractCalibrationViewModel is not null)
            {
                ActiveItem = abstractCalibrationViewModel;
            }
            else
            {
                ActiveItem = null;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{@Name}: Load Management View({@ViewModel}) Failed", nameof(SystemManagementViewModel), viewModel);
        }
    }

    [RelayCommand]
    private void Close() => CloseView(true);

    [RelayCommand]
    private void Update() => ActiveItem?.Update();

    [RelayCommand]
    private void Insert() => ActiveItem?.Insert();

    [RelayCommand]
    private Task DeleteAsync() => ActiveItem?.DeleteAsync() ?? Task.CompletedTask;

    [RelayCommand]
    private Task SelectWithConditionAsync() => ActiveItem?.SelectAsync() ?? Task.CompletedTask;

    [RelayCommand(IncludeCancelCommand = true)]
    private Task SelectAllAsync(CancellationToken cancellationToken) => ActiveItem?.SelectAllAsync() ?? Task.CompletedTask;

    [RelayCommand]
    private void Reset() => ActiveItem?.Reset();
}
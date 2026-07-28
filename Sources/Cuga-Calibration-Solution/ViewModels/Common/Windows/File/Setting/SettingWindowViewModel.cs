using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Setting;
using Net.Utilities.SourceGenerators.Calibration.Attributes;
using CugaCalibration.ViewModels.Common.Windows.File.Setting.Children;
using Local.SQL.Cache.Providers.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.Services;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using System.Windows;
using System.Windows.Controls;

namespace CugaCalibration.ViewModels.Common.Windows.File.Setting;

[IOCAppService(ServiceType = typeof(SettingWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Transient)]
public sealed partial class SettingWindowViewModel(
    CalibrationSetting calibrationSetting,
    ApplicationCookie applicationCookie,
    ICacheProvider cacheProvider,
    IDialogWindowProvider dialogWindowProvider,
    IWindowManagerService windowManagerService,
    ILogger<SettingWindowViewModel> logger) : ViewModelBase
{
    [DefaultCache]
    [ObservableProperty]
    public partial CalibrationSetting CalibrationSetting { get; set; } = calibrationSetting;

    [ObservableProperty]
    public partial ApplicationCookie ApplicationCookie { get; set; } = applicationCookie;

    [ObservableProperty]
    private SettingDisableCalibrationConfigViewModel _settingDisableCalibrationConfigViewModel = HostApplication.GetRequiredService<SettingDisableCalibrationConfigViewModel>();

    [ObservableProperty]
    private SettingCalibrateItemsStatusViewModel _settingCalibrateItemsStatusViewModel = HostApplication.GetRequiredService<SettingCalibrateItemsStatusViewModel>();

    [ObservableProperty]
    public partial SettingRequiredCalibrationViewModel SettingRequiredCalibrationViewModel { get; set; } = HostApplication.GetRequiredService<SettingRequiredCalibrationViewModel>();

    [ObservableProperty]
    public partial SettingRelationCalibrationViewModel SettingRelationCalibrationViewModel { get; set; } = HostApplication.GetRequiredService<SettingRelationCalibrationViewModel>();

    /// <summary>
    /// 控制 SettingDisableCalibrateConfigUserControl 的启用状态
    /// </summary>
    [ObservableProperty]
    private bool _isDisableConfigEnabled;

    /// <summary>
    /// 控制 TabControl 的启用状态，弹窗期间禁用切换
    /// </summary>
    [ObservableProperty]
    private bool _isTabControlEnabled = true;

    /// <summary>
    /// 处理 TabControl 的 SelectionChanged 事件
    /// </summary>
    [RelayCommand]
    private async Task HandleTabSelectionChangedAsync(SelectionChangedEventArgs args)
    {
        // 检查是否是 TabControl 直接触发的事件，避免嵌套控件的事件冒泡
        if (args.OriginalSource is not TabControl)
            return;

        // 检查新选中的标签页
        if (args.AddedItems.Count == 0)
            return;

        if (args.AddedItems[0] is not TabItem tabItem)
            return;

        // 获取 TabItem 的 Content（UserControl）的 DataContext
        if (tabItem.Content is not FrameworkElement element)
            return;

        var dataContext = element.DataContext;

        if (dataContext == null)
            return;

        // 判断 DataContext 是否为 SettingDisableCalibrationConfigViewModel 类型
        if (dataContext is SettingDisableCalibrationConfigViewModel)
        {
            IsTabControlEnabled = false;
            try
            {
                // 每次切换到该标签页都弹出配置管理窗口（Transient 生命周期，每次都是新实例）
                var manageDisableCalibrationWindowViewModel = HostApplication.GetRequiredService<ManageDisableCalibrationWindowViewModel>();

                // 等待加载数据完成后再显示对话框，避免竞态条件
                await manageDisableCalibrationWindowViewModel.LoadedCommand.ExecuteAsync(null);

                // 显示对话框
                var dialogResult = windowManagerService.ShowDialog(manageDisableCalibrationWindowViewModel);

                // 根据对话框结果设置启用状态
                // true = 用户点击了 Edit 按钮，允许显示配置
                // false/null = 用户取消或关闭窗口，保持禁用状态
                IsDisableConfigEnabled = dialogResult == true;
            }
            finally
            {
                IsTabControlEnabled = true;
            }
        }
        // 判断 DataContext 是否为 SettingCalibrateItemsStatusViewModel 类型
        else if (dataContext is SettingCalibrateItemsStatusViewModel)
        {
            IsTabControlEnabled = false;
            try
            {
                // 每次切换到该标签页都执行加载逻辑
                await SettingCalibrateItemsStatusViewModel.LoadedCommand.ExecuteAsync(null);
            }
            finally
            {
                IsTabControlEnabled = true;
            }
        }
    }

    [RelayCommand]
    private void Restore()
    {
        try
        {
            CalibrationSetting.AdaptIn(cacheProvider.GetOrDefault<CalibrationSetting>());
        }
        catch (Exception ex)
        {
            dialogWindowProvider.ShowDialog($"""
                                             Restore Failed
                                             {ex.Message}
                                             """, DialogButtonsEnum.OK, DialogIconEnum.Warning);
            logger.LogError(ex, "Restore Setting");
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            if (await SettingCalibrateItemsStatusViewModel.SavingAsync().ConfigureAwait(false) == false)
            {
                dialogWindowProvider.ShowDialog("Save Calibrations Enable Status Failed, Please Check Settings and try again!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return;
            }

            if (await SettingRequiredCalibrationViewModel.SavingAsync().ConfigureAwait(false) == false)
            {
                dialogWindowProvider.ShowDialog("Save Calibrations Enable Status Failed, Please Check Settings and try again!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return;
            }

            if (await SettingDisableCalibrationConfigViewModel.SavingAsync().ConfigureAwait(false) == false)
            {
                dialogWindowProvider.ShowDialog("Save Disable Calibration Config Failed, Please Check Settings and try again!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return;
            }

            if (await SettingRelationCalibrationViewModel.SavingAsync().ConfigureAwait(false) == false)
            {
                dialogWindowProvider.ShowDialog("Save Disable Calibration Config Failed, Please Check Settings and try again!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return;
            }

            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            cacheProvider.Set(CalibrationSetting, cancellationTokenSource.Token);

            CloseView(true);
        }
        catch (Exception ex)
        {
            dialogWindowProvider.ShowDialog($"""
                                             Save Failed
                                             {ex.Message}
                                             """, DialogButtonsEnum.OK, DialogIconEnum.Warning);
            logger.LogError(ex, "Save Setting");
        }
    }

    [RelayCommand]
    private void Close()
    {
        SettingCalibrateItemsStatusViewModel.Closing();
        SettingRelationCalibrationViewModel.Closing();
        CloseView(true);
    }
}
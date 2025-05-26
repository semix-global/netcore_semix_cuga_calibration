using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Models.Setting;
using CugaCalibration.Core.Models;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.WPF.MVVM;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using System.Reflection;
using ApplicationCookie = CugaCalibration.Core.Models.ApplicationCookie;

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
    private void Loaded()
    {
        var allManageList = new List<SystemManageMenu>();
        var rootNameSpace = typeof(SystemManagementViewModel).Namespace;
        var assembly = Assembly.GetExecutingAssembly();
        foreach (var (type, _) in from type in assembly.GetTypes().Where(t => t.FullName.StartsWith(rootNameSpace) && !t.FullName.Contains('+'))
                                  let namespaceStr = type.FullName
                                  select (type, namespaceStr))
        {
            if (!type.IsSubclassOf(typeof(ManagementViewModelBase)))
                continue;
            var viewmodel = (ManagementViewModelBase)HostApplication.GetRequiredService(type);
            var menuItem = new SystemManageMenu
            {
                DisplayName = viewmodel.DisplayName,
                OrderNum = viewmodel.DisplayOrderNum,
                Component = type.FullName
            };
            if (!allManageList.Contains(menuItem))
                allManageList.Add(menuItem);
        }

        var managementMenuList = allManageList.Select(t => t.Component[0..t.Component.LastIndexOf('.')]).Distinct()
            .Select((t, i) => new SystemManageMenu { OrderNum = i, DisplayName = t.Split('.').Last(), Component = t }).OrderBy(t => t.OrderNum).ToList();
        if (managementMenuList is null) return;
        foreach (var item in managementMenuList)
        {
            var groupChildrenList = allManageList.Where(t => t.Component.StartsWith(item.Component)).OrderBy(t => t.OrderNum).ToList();
            item.ChildList = groupChildrenList;
        }

        ApplicationCookie.SystemManageMenuList = managementMenuList;
        return;
    }

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
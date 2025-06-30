using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Utilities;
using Local.SQL.DB.Providers.Models.Entities.DTO;
using Local.SQL.DB.Providers.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.IOC.Providers;
using Net.Utilities.WPF.MVVM.Providers;
using System.Collections.ObjectModel;

namespace CugaCalibration.ViewModels.Common.Windows.Management.Permission;

[IOCAppService(ServiceType = typeof(MenusManagementViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class MenusManagementViewModel(
    ISysMenuService sysMenuService,
    ILogger<ManagementViewModelBase> logger,
    IDialogWindowProvider dialogWindowProvider,
    ISynchronizationContextProvider contextProvider) : ManagementViewModelBase(logger, dialogWindowProvider, contextProvider)
{
    #region 属性

    public override int DisplayOrderNum => 2;

    public override string DisplayName => "Menus";

    public override bool IsEnableEdit => false;

    private List<SysMenuDto> menuList = [];

    #endregion 属性

    #region viewmodel

    [ObservableProperty]
    private ObservableCollection<SysMenuDto> _menuTreeList = [];

    [ObservableProperty]
    private SysMenuDto? _operateSysMenuDto;

    #endregion viewmodel

    #region 重载

    protected override Task<bool> LoadedingAsync()
    {
        SynchronizationContextProvider.Send(() => MenuTreeList.Clear());
        return Task.FromResult(true);
    }

    protected override async Task<bool> SelectingAsync()
    {
        var selectDto = OperateSysMenuDto!.Clone();
        var result = await sysMenuService.GetByConditionAsync(selectDto).ConfigureAwait(false);
        SynchronizationContextProvider.Send(() =>
        {
            MenuTreeList.Clear();
            MenuTreeList.AddRange(result);
        });
        return true;
    }

    protected override async Task<bool> SelectingAllAsync()
    {
        menuList = await sysMenuService.GetAllAsync().ConfigureAwait(false);
        var treeListResult = sysMenuService.BuildMenuTree(menuList);
        SynchronizationContextProvider.Send(() =>
        {
            MenuTreeList.Clear();
            MenuTreeList.AddRange(treeListResult);
        });
        return true;
    }

    protected override void VariableInitialization() => OperateSysMenuDto = new SysMenuDto();

    #endregion 重载

    #region Command

    [RelayCommand]
    private async Task ApplyCommandAsync() => await ApplyAsync().ConfigureAwait(false);

    [RelayCommand]
    private void Cancel() => UpdateEnableStatus(true);

    #endregion Command
}
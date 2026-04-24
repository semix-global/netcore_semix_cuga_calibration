using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Local.SQL.DB.Providers.Models.Entities.DTO;
using Local.SQL.DB.Providers.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.IOC.Providers;
using Net.Utilities.WPF.MVVM.Providers;
using System.Collections.ObjectModel;

namespace CugaCalibration.ViewModels.Common.Windows.Management.Permission;

[IOCAppService(ServiceType = typeof(RolesManagementViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class RolesManagementViewModel(
    ISysRoleService sysRoleService,
    ISysMenuService sysMenuService,
    ILogger<ManagementViewModelBase> logger,
    IDialogWindowProvider dialogWindowProvider,
    ISynchronizationContextProvider contextProvider) : ManagementViewModelBase(logger, dialogWindowProvider, contextProvider)
{
    #region 属性

    public override int DisplayOrderNum => 3;

    public override string DisplayName => "Roles";

    public override bool IsSelectedItem => SelectSysRoleDto is not null && SelectSysRoleDto.Id != 0;

    private List<SysMenuDTO> allMenuList = [];

    #endregion 属性

    #region viewmodel

    /// <summary>
    /// treeview显示模式，0：默认  1：分配菜单模式
    /// </summary>
    [ObservableProperty]
    private int _isAssignMenu;

    [ObservableProperty]
    private ObservableCollection<SysRoleDTO> _roleList = [];

    [ObservableProperty]
    private SysRoleDTO? _selectSysRoleDto = new();

    [ObservableProperty]
    private SysRoleDTO? _operateSysRoleDto = new();

    [ObservableProperty]
    private ObservableCollection<SysMenuDTO> _menuAllocationTreeList = [];

    #endregion viewmodel

    #region 重载

    protected override async Task<bool> LoadedingAsync()
    {
        SynchronizationContextProvider.Send(() => RoleList.Clear());
        allMenuList = await sysMenuService.GetAllAsync().ConfigureAwait(false);
        MenuAllocationTreeList = [.. sysMenuService.BuildMenuTree(allMenuList)];
        sysMenuService.EnableCascadeSave(true);
        sysRoleService.EnableCascadeSave(true);
        return true;
    }

    protected override async Task<bool> InsertingAsync()
    {
        OperateSysRoleDto!.SysMenuList = [.. allMenuList.Where(t => t.IsDistributed)];
        return await sysRoleService.InsertAsync(OperateSysRoleDto!).ConfigureAwait(false);
    }

    protected override async Task<bool> DeletingingAsync()
    {
        return await sysRoleService.DeleteAsync(SelectSysRoleDto!).ConfigureAwait(false);
    }

    protected override async Task<bool> UpdatingAsync()
    {
        return await sysRoleService.UpdateAsync(OperateSysRoleDto!).ConfigureAwait(false);
    }

    protected override async Task<bool> SelectingAsync()
    {
        var selectDto = OperateSysRoleDto!.Clone();
        var result = await sysRoleService.GetByConditionAsync(selectDto).ConfigureAwait(false);
        SynchronizationContextProvider.Send(() =>
        {
            RoleList.Clear();
            RoleList.AddRange(result);
        });
        return true;
    }

    protected override async Task<bool> SelectingAllAsync()
    {
        var result = await sysRoleService.GetAllAsync().ConfigureAwait(false);
        SynchronizationContextProvider.Send(() =>
        {
            RoleList.Clear();
            RoleList.AddRange(result);
        });
        return true;
    }

    protected override async Task<bool> ApplyingAsync()
    {
        return await InsertingAsync().ConfigureAwait(false);
    }

    protected override void VariableInitialization()
    {
        switch (OperateCommandIndex)
        {
            case 1:
                OperateSysRoleDto = SelectSysRoleDto!.Clone();
                break;

            case 0 or 2:
                OperateSysRoleDto = new SysRoleDTO();
                IsAssignMenu = 0;
                break;
        }
    }

    protected override Task<bool> RefreshAssociationTableAsync()
    {
        foreach (var t in allMenuList)
        {
            t.IsDistributed = OperateSysRoleDto!.SysMenuList
                .Where(roleMenu => roleMenu.Id == t.Id) //role-menu关联表加载
                .ToList().Count != 0;
        }

        MenuAllocationTreeList = [.. sysMenuService.BuildMenuTree(allMenuList)];
        return Task.FromResult(true);
    }

    #endregion 重载

    #region Command

    [RelayCommand]
    private async Task ApplyCommandAsync() => await ApplyAsync().ConfigureAwait(false);

    [RelayCommand]
    private void Cancel()
    {
        UpdateEnableStatus(true);
        VariableInitialization();
    }

    [RelayCommand]
    private void AssignMenu()
    {
        IsAssignMenu = 1;
    }

    #endregion Command
}
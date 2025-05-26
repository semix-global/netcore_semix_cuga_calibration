using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Local.SQL.DB.Providers.Models.Entities.DTO;
using Local.SQL.DB.Providers.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Constants;
using Net.Utilities.Enums;
using Net.Utilities.Helper.IOC.Providers;
using Net.Utilities.Helper.Object.String;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM.Providers;
using System.Collections.ObjectModel;

namespace CugaCalibration.ViewModels.Common.Windows.Management.Permission;

[IOCAppService(ServiceType = typeof(UsersManagementViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class UsersManagementViewModel(
    ILogger<ManagementViewModelBase> logger,
    IDialogWindowProvider dialogWindowProvider,
    ISynchronizationContextProvider synchronizationContextProvider,
    ISysUserService sysUserService,
    ISysDeptService sysDeptService,
    ISysRoleService sysRoleService)
    : ManagementViewModelBase(logger,
        dialogWindowProvider,
        synchronizationContextProvider)
{
    #region 属性

    public override int DisplayOrderNum => 0;

    public override string DisplayName => "Users";

    public override bool IsSelectedItem => SelectSysUserDto is not null && SelectSysUserDto.Id != 0;

    #endregion 属性

    #region viewmodel

    [ObservableProperty]
    private ObservableCollection<SysUserDto> _userList = [];

    [ObservableProperty]
    private ObservableCollection<SysDeptDto> _deptList = [];

    [ObservableProperty]
    private SysUserDto? _selectSysUserDto = new();

    [ObservableProperty]
    private SysUserDto? _operateSysUserDto = new();

    [ObservableProperty]
    private SysDeptDto? _selectSysDeptDto = new();

    [ObservableProperty]
    private ObservableCollection<RoleAllocation> _roleAllocationList = [];

    #endregion viewmodel

    #region 重载

    protected override async Task<bool> LoadedingAsync()
    {
        SynchronizationContextProvider.Send(() => UserList.Clear());
        DeptList = [.. await sysDeptService.GetAllAsync().ConfigureAwait(false)];
        sysUserService.EnableCascadeSave(true);
        sysDeptService.EnableCascadeSave(true);
        sysRoleService.EnableCascadeSave(true);
        return true;
    }

    protected override async Task<bool> InsertingAsync()
    {
        OperateSysUserDto!.SysRoleList = [.. RoleAllocationList.Where(t => t.IsSelected).Select(t => t.RoleDto)]; //角色分配
        OperateSysUserDto!.DeptId = SelectSysDeptDto!.Id;
        if (OperateCommandIndex == 2) OperateSysUserDto.Password = MD5Encrypt.Encrypt32(OperateSysUserDto.Password?.ToString() ?? "666666");
        return await sysUserService.InsertAsync(OperateSysUserDto).ConfigureAwait(false);
    }

    protected override async Task<bool> DeletingingAsync()
    {
        return await sysUserService.DeleteAsync(SelectSysUserDto!).ConfigureAwait(false);
    }

    protected override async Task<bool> SelectingAsync()
    {
        var selectDto = OperateSysUserDto!.Clone();
        selectDto.DeptId = SelectSysDeptDto is null
            ? ConstantHelper.NegValue
            : SelectSysDeptDto!.Id;
        var result = await sysUserService.GetByConditionAsync(selectDto).ConfigureAwait(false);
        SynchronizationContextProvider.Send(() =>
        {
            UserList.Clear();
            UserList.AddRange(result);
        });
        //UserList = new ObservableCollection<SysUserDto>(result);
        return true;
    }

    protected override async Task<bool> SelectingAllAsync()
    {
        var result = await sysUserService.GetAllAsync().ConfigureAwait(false);
        SynchronizationContextProvider.Send(() =>
        {
            UserList.Clear();
            UserList.AddRange(result);
        });
        return true;
    }

    protected override async Task<bool> ApplyingAsync()
    {
        if (SelectSysDeptDto is null || SelectSysDeptDto.Id == 0)
        {
            DialogWindowProvider.ShowDialog("Apply setting Failed", "Please select a department.", DialogButtonsEnum.RetryCancel, DialogIconEnum.Warning);
            return false;
        }

        return await InsertingAsync().ConfigureAwait(false);
    }

    protected override void VariableInitialization()
    {
        switch (OperateCommandIndex)
        {
            case 0 or 2:
                OperateSysUserDto = new SysUserDto();
                SelectSysDeptDto = null;
                break;

            case 1:
                OperateSysUserDto = SelectSysUserDto!.Clone();
                SelectSysDeptDto = DeptList.SingleOrDefault(s => s.Id == OperateSysUserDto!.DeptId);
                break;
        }
    }

    protected override async Task<bool> RefreshAssociationTableAsync()
    {
        var roleList = new ObservableCollection<SysRoleDto>(await sysRoleService.GetAllAsync().ConfigureAwait(false));
        RoleAllocationList =
        [
            .. roleList.Select(t =>
                new RoleAllocation
                {
                    IsSelected = OperateSysUserDto!.SysRoleList.Where(userRole => userRole.Id == t.Id).ToList().Count != 0, //user-role关联表加载
                    RoleDto = t
                }).ToList()
        ];
        return true;
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

    #endregion Command

    public sealed partial class RoleAllocation : ObservableObject
    {
        [ObservableProperty]
        private bool _isSelected;

        [ObservableProperty]
        private SysRoleDto _roleDto = new();
    }
}
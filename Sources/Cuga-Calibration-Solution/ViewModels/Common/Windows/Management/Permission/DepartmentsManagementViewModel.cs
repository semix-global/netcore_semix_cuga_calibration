using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Local.SQL.DB.Providers.Models.Entities.DTO;
using Local.SQL.DB.Providers.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Constants;
using Net.Utilities.Enums;
using Net.Utilities.Helper.IOC.Providers;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM.Providers;
using System.Collections.ObjectModel;
using Net.Utilities.Extensions;

namespace CugaCalibration.ViewModels.Common.Windows.Management.Permission;

[IOCAppService(ServiceType = typeof(DepartmentsManagementViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class DepartmentsManagementViewModel(
    ISysDeptService sysDeptService,
    ISysUserService sysUserService,
    ILogger<ManagementViewModelBase> logger,
    IDialogWindowProvider dialogWindowProvider,
    ISynchronizationContextProvider contextProvider) : ManagementViewModelBase(logger, dialogWindowProvider, contextProvider)
{
    #region 属性

    public override int DisplayOrderNum => 1;

    public override string DisplayName => "Departments";

    public override bool IsSelectedItem => SelectSysDeptDto is not null && SelectSysDeptDto.Id != 0;

    private List<SysDeptDto> deptList = [];

    /// <summary>
    /// 设置父级部门时使用，控制界面禁用状态
    /// </summary>
    [ObservableProperty]
    private bool _isTreeViewEnabled = true;

    #endregion 属性

    #region viewmodel

    [ObservableProperty]
    private ObservableCollection<SysDeptDto> _deptTreeList = [];

    [ObservableProperty]
    private List<SysUserDto> _userList = [];

    [ObservableProperty]
    private SysDeptDto? _operateSysDeptDto;

    [ObservableProperty]
    private SysDeptDto? _selectSysDeptDto = new();

    [ObservableProperty]
    private SysUserDto? _selectSysUserDto = new();

    #endregion viewmodel

    #region 重载

    protected override async Task<bool> LoadedingAsync()
    {
        SynchronizationContextProvider.Send(() => DeptTreeList.Clear());
        UserList = await sysUserService.GetAllAsync().ConfigureAwait(false);
        sysDeptService.EnableCascadeSave(true);
        return true;
    }

    protected override async Task<bool> InsertingAsync()
    {
        OperateSysDeptDto!.LeaderUserId = SelectSysUserDto!.Id;
        OperateSysDeptDto!.Leader = SelectSysUserDto!.UserName;
        return await sysDeptService.InsertAsync(OperateSysDeptDto!).ConfigureAwait(false);
    }

    protected override async Task<bool> DeletingingAsync()
    {
        return await Task.Run(() => sysDeptService.Delete(SelectSysDeptDto!)).ConfigureAwait(false);
    }

    protected override async Task<bool> SelectingAsync()
    {
        var selectDto = OperateSysDeptDto!.Clone();
        selectDto.LeaderUserId = SelectSysUserDto is null ? ConstantHelper.NegValue : SelectSysUserDto!.Id;
        selectDto.Leader = SelectSysUserDto is null ? string.Empty : SelectSysUserDto.UserName;

        var result = await sysDeptService.GetByConditionAsync(selectDto).ConfigureAwait(false);
        SynchronizationContextProvider.Send(() =>
        {
            DeptTreeList.Clear();
            DeptTreeList.AddRange(result);
        });
        deptList = [.. result.Select(t => t.Clone())];
        return true;
    }

    protected override async Task<bool> SelectingAllAsync()
    {
        deptList = await sysDeptService.GetAllAsync().ConfigureAwait(false);
        var treeListResult = sysDeptService.BuildDepartmentTree(deptList);
        SynchronizationContextProvider.Send(() =>
        {
            DeptTreeList.Clear();
            DeptTreeList.AddRange(treeListResult);
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
            case 0:
                OperateSysDeptDto = new SysDeptDto();
                SelectSysUserDto = null;
                break;

            case 1: //edit
                SelectSysUserDto = UserList.SingleOrDefault(s => s.Id == SelectSysDeptDto!.LeaderUserId);
                SelectSysUserDto ??= new SysUserDto();
                OperateSysDeptDto = SelectSysDeptDto!.Clone();
                OperateSysDeptDto.LeaderUserId = SelectSysUserDto!.Id;
                OperateSysDeptDto.Leader = SelectSysUserDto!.UserName;
                break;

            case 2: //add
                OperateSysDeptDto = new SysDeptDto { Parent = SelectSysDeptDto!, ParentId = SelectSysDeptDto!.Id == 0 ? ConstantHelper.NegValue : SelectSysDeptDto!.Id };
                SelectSysUserDto = new SysUserDto();
                break;
        }

        IsTreeViewEnabled = OperateCommandIndex == 0;
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
    private void Selection(SysDeptDto item)
    {
        if (OperateCommandIndex == 0)
            SelectSysDeptDto = item;
    }

    [RelayCommand]
    private void SetParentDepartment(SysDeptDto item)
    {
        if (OperateCommandIndex != 0 && IsTreeViewEnabled)
        {
            if (item.Name == OperateSysDeptDto!.Name)
            {
                DialogWindowProvider.ShowDialog("The parent and child are the same object!", DialogButtonsEnum.OKCancel, DialogIconEnum.Warning);
                return;
            }

            OperateSysDeptDto!.Parent = item;
            OperateSysDeptDto!.ParentId = OperateSysDeptDto.Parent!.Id;
            IsTreeViewEnabled = false;
        }
    }

    [RelayCommand]
    private void ToggleSetParentDepartmentMode()
    {
        IsTreeViewEnabled = !IsTreeViewEnabled;
    }

    [RelayCommand]
    private void SetDefaultParentDepartment()
    {
        DialogWindowProvider.TryShowDialog("Should the first level department be designated as the parent department?", out var dialogButtonsEnum, DialogButtonsEnum.YesNo, DialogIconEnum.Warning);
        if (dialogButtonsEnum == DialogResultEnum.No)
            return;

        OperateSysDeptDto!.Parent = null;
        OperateSysDeptDto!.ParentId = ConstantHelper.NegValue;
    }

    #endregion Command
}
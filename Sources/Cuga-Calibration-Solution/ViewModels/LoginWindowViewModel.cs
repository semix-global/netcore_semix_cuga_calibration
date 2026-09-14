using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CugaCalibration.Core.Services.Interfaces;
using CugaCalibration.ViewModels.Common;
using Local.SQL.DB.Providers.Models.Entities.DTO;
using Local.SQL.DB.Providers.Models.Exceptions;
using Local.SQL.DB.Providers.Services.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Calibration;
using Net.Utilities.Enums;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;

namespace CugaCalibration.ViewModels;

[IOCAppService(ServiceType = typeof(LoginWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public partial class LoginWindowViewModel(
    ISysUserService sysUserService,
    ISysUserRoleService sysUserRoleService,
    ILogger<LoginWindowViewModel> logger,
    IDialogWindowProvider dialogWindowProvider,
    IHostEnvironment hostEnvironment,
    ConfigViewModel configViewModel,
    string applicationName,
    IApplicationCookieService applicationCookieService) : ViewModelBase
{
    [ObservableProperty]
    public partial string Title { get; set; } = applicationName;

    [ObservableProperty]
    public partial IReadOnlyList<SysUserDTO> Users { get; set; } = [];

    /// <summary>
    /// 下拉框当前选中的用户 (Users 列表实例, 仅承载界面选中状态)
    /// </summary>
    [ObservableProperty]
    public partial SysUserDTO? SelectedUser { get; set; }

    /// <summary>
    /// 登录输入载体: 用户名 (下拉选中或手动输入) + 密码 (手动输入), 不直接引用 Users 列表实例
    /// </summary>
    [ObservableProperty]
    public partial SysUserDTO SysUserDTO { get; set; } = new();

    /// <summary>
    /// 选中用户变化时同步用户名到登录输入载体, 避免直接编辑列表实例造成引用污染
    /// </summary>
    partial void OnSelectedUserChanged(SysUserDTO? value)
    {
        if (value is null) return;

        SysUserDTO.UserName = value.UserName;
    }

    [RelayCommand]
    private async Task LoadedAsync()
    {
        try
        {
            if (configViewModel.Connect() == false)
            {
                dialogWindowProvider.ShowDialog("Login Failed: Connect Login Service Failed", DialogButtonsEnum.OK, DialogIconEnum.Warning);

                return;
            }

            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(20));

            var cugaRegisterUsers = await configViewModel.GetRegisteredUsersInformationAsync(cancellationTokenSource.Token).ConfigureAwait(false);

            var allUsers = await sysUserService.GetAllAsync(cancellationTokenSource.Token).ConfigureAwait(false);
            foreach (var registerUser in cugaRegisterUsers.Where(t => t.IsAdmin == false))
            {
                var user = allUsers.SingleOrDefault(t => t.UserName == registerUser.UserName);
                if (user is null)
                {
                    if (await sysUserService.InsertAsync(registerUser, cancellationTokenSource.Token).ConfigureAwait(false) == false) ThrowHelper.ThrowArgumentException<SysUserDTO>("Insert cuga register user failed!");

                    if (await sysUserRoleService.InsertAsync(registerUser, cancellationTokenSource.Token).ConfigureAwait(false) == false) ThrowHelper.ThrowArgumentException<SysUserDTO>("Insert cuga register user Roles failed!");
                }
                else
                {
                    user.Password = registerUser.Password; // 密码已经hash

                    if (await sysUserService.UpdateAsync(user, cancellationTokenSource.Token).ConfigureAwait(false) == false) ThrowHelper.ThrowArgumentException<SysUserDTO>("Update cuga register user failed!");
                }
            }

            await applicationCookieService.LoadingRoleMenuDataAsync(cancellationTokenSource.Token);

            Users = [.. (await sysUserService.GetAllAsync(CancellationToken.None).ConfigureAwait(false)).OrderBy(t => t.Id)];

            if (hostEnvironment.IsProduction())
            {
                SelectedUser = Users.FirstOrDefault(t => t.SysRoleList.Any(tt => tt.Id == 2));
            }
            else
            {
                SysUserDTO = new SysUserDTO { UserName = "SuperAdmin", Password = "666666", Id = 1 };
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{@Name}: LoadedAsync", nameof(LoginWindowViewModel));

            CloseView(false);
        }
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task LoginAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Run(async () =>
            {
                // 登录用户对象: 按界面用户名从用户列表解析 (覆盖下拉选中与手动输入两种情形), 解析不到则仅携带输入内容; 密码均取界面输入
                var loginSysUserDto = Users.FirstOrDefault(t => t.UserName == SysUserDTO.UserName)?.Clone() ?? new SysUserDTO { UserName = SysUserDTO.UserName };
                loginSysUserDto.Password = SysUserDTO.Password;

                var tempSysUserDto = await configViewModel.LoginAsync(loginSysUserDto, cancellationToken).ConfigureAwait(false);

                await applicationCookieService.LoadingSystemMenuCookieAsync(tempSysUserDto, cancellationToken).ConfigureAwait(false);

                CloseView(true);
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (ex is LoginException loginException)
            {
                dialogWindowProvider.ShowDialog($"Login Failed: {loginException.Message}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return;
            }

            logger.LogError(ex, "{@Name}: Login failed", nameof(LoginWindowViewModel));
            dialogWindowProvider.ShowDialog($"Login Failed: {ex}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
        }
    }

    [RelayCommand]
    private void Close()
    {
        LoginCancelCommand.Execute(null);

        CloseView(false);
    }
}
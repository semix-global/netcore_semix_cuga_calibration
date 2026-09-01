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
using Net.Utilities.Enums;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;

namespace CugaCalibration.ViewModels;

[IOCAppService(ServiceType = typeof(LoginWindowViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public partial class LoginWindowViewModel(
    ISysUserService sysUserService,
    ISysRoleService sysRoleService,
    ISysMenuService sysMenuService,
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
    public partial IReadOnlyList<string> UserNames { get; set; } = [];

    [ObservableProperty]
    public partial SysUserDTO SysUserDTO { get; set; } = new();

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

            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(150000));
            sysUserService.EnableCascadeSave(true);

            var cugaRegisterUsers = configViewModel.GetRegisteredUsersInformation();

            var allUsers = await sysUserService.GetAllAsync(cancellationTokenSource.Token).ConfigureAwait(false);
            var adminUser = allUsers.First(t => t.IsAdmin);

            foreach (var registerUser in cugaRegisterUsers)
            {
                SysUserDTO userDTO;
                var user = allUsers.SingleOrDefault(t => t.UserName == registerUser.UserName);

                if (user != null)
                {
                    // 已存在用户: 仅同步基本信息
                    user.Password = registerUser.Password;
                    userDTO = user;
                }
                else
                {
                    // 首次加入的用户: 分配 admin 角色
                    registerUser.SysRoleList = [adminUser.SysRoleList.Single(t => t.IsAdmin)];
                    userDTO = registerUser;
                }

                if (await sysUserService.InsertAsync(userDTO, cancellationTokenSource.Token).ConfigureAwait(false) == false)
                    ThrowHelper.ThrowArgumentException<SysUserDTO>("Insert cuga register user failed!");
            }

            var users = await sysUserService.GetAllAsync(CancellationToken.None).ConfigureAwait(false);

            UserNames = [.. users.OrderBy(t => t.Id).Select(t => t.UserName)];

            Guard.IsNotEmpty(users, "The user list is empty, please register a user first!");

            SysUserDTO = hostEnvironment.IsProduction() ? new SysUserDTO { UserName = "Admin", Password = "FxEiw4b0" } : new SysUserDTO { UserName = "Admin", Password = "666666" };
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
                var tempSysUserDto = await configViewModel.LoginAsync(SysUserDTO, cancellationToken).ConfigureAwait(false);

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
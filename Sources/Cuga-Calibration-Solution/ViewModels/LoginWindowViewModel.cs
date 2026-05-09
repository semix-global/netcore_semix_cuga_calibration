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
    ILogger<LoginWindowViewModel> logger,
    IDialogWindowProvider dialogWindowProvider,
    IHostEnvironment hostEnvironment,
    ConfigViewModel configViewModel,
    string applicationName,
    IApplicationCookieService applicationCookieService) : ViewModelBase
{
    [ObservableProperty]
    private string _title = applicationName;

    [ObservableProperty]
    private IReadOnlyList<string> _userNames = [];

    [ObservableProperty]
    private SysUserDTO _sysUserDTO = new();

    [RelayCommand]
    private async Task LoadedAsync()
    {
        try
        {
            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            var users = await sysUserService.GetAllAsync(cancellationTokenSource.Token).ConfigureAwait(false);

            UserNames = [.. users.OrderBy(t => t.Id).Select(t => t.UserName)];

            if (users.Count > 0) SysUserDTO = hostEnvironment.IsProduction() ? new SysUserDTO { UserName = users[0].UserName, Password = "t5sne0yd" } : new SysUserDTO { UserName = users[0].UserName, Password = "666666" };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{@Name}: LoadedAsync", nameof(LoginWindowViewModel));
        }
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task LoginAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Run(async () =>
            {
                if (configViewModel.Connect() == false)
                {
                    dialogWindowProvider.ShowDialog("Login Failed: Connect Login Service Failed", DialogButtonsEnum.OK, DialogIconEnum.Warning);

                    return;
                }

                var tempSysUserDto = await configViewModel.LoginAsync(SysUserDTO, cancellationToken).ConfigureAwait(false);

                await applicationCookieService.UpdateCookieAsync(tempSysUserDto, cancellationToken).ConfigureAwait(false);

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
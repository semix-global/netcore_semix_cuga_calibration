using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Core.Models.Enums.Stage;
using Core.Models.Events;
using Core.Models.Helper;
using Core.Models.Models.Common.Alignment;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Common.Pattern;
using Core.Recipe.Models;
using CugaCalibration.ViewModels.Common.Windows.Tools;
using CugaCalibration.ViewModels.Common.Windows.Tools.Alignment;
using Local.SQL.Cache.Providers.Bases;
using Local.SQL.Cache.Providers.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.IOC.Providers;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.Services;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;

namespace CugaCalibration.ViewModels.Common.Windows.Recipe.Edit.Children;

[IOCAppService(ServiceType = typeof(RecipeAlignmentViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class RecipeAlignmentViewModel(
    [FromKeyedServices(CalibrationConstantsHelper.RecipeDbKey)]
    ICacheProvider recipeCacheProvider,
    IDialogWindowProvider dialogWindowProvider,
    IWindowManagerService windowManagerService,
    IMessenger messenger,
    ISynchronizationContextProvider contextProvider,
    AlignmentWindowBrightFieldViewModel alignmentWindowBrightFieldViewModel,
    AlignmentWindowDarkFieldViewModel alignmentWindowDarkFieldViewModel,
    FindWaferCenterByManuallyWindowViewModel findWaferCenterByManuallyWindowViewModel,
    ApplicationCookie applicationCookie) : ViewModelBase
{
    private bool _isCircleCenterUpdatedInSession;
    private bool _isAlignmentResultUpdatedInSession;

    // 对外暴露给主 VM 协调用
    public ApplicationCookie ApplicationCookie => applicationCookie;
    public CalibrationRecipeDTO? EditingDTO { get; set; }

    [ObservableProperty]
    public partial AlignmentFindCenterCache AlignmentFindCenterCache { get; set; } = new();

    [ObservableProperty]
    public partial Cache Cache { get; set; } = new();

    [ObservableProperty]
    public partial AlignmentUserControlViewModel AlignmentUserControlViewModel { get; set; } = HostApplication.GetRequiredService<AlignmentUserControlViewModel>();

    public required FindWaferCenterByManuallyWindowViewModel FindWaferCenterByManuallyWindowViewModel { get; set; }

    public async Task InitializeAsync(CalibrationRecipeDTO calibrationRecipeDTO)
    {
        EditingDTO = calibrationRecipeDTO;

        _isCircleCenterUpdatedInSession = false;
        _isAlignmentResultUpdatedInSession = false;

        Cache = recipeCacheProvider.GetOrDefault<Cache>();

        FindWaferCenterByManuallyWindowViewModel = findWaferCenterByManuallyWindowViewModel;
        await FindWaferCenterByManuallyWindowViewModel.FindWaferCenterLoadedAsync().ConfigureAwait(false);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task FindWaferCenterAsync(CancellationToken cancellationToken)
    {
        Guard.IsNotNull(EditingDTO);

        messenger.Send(ToggleRecipeEventFactory.UpdateIsWaferMapEditEnable(false));

        await Task.Run(async () =>
        {
            try
            {
                FindWaferCenterByManuallyWindowViewModel.Cache.PositionErrorThreshold = Cache.PositionErrorThreshold;

                var result = await FindWaferCenterByManuallyWindowViewModel.ActionAsync(cancellationToken);
                if (result == false) dialogWindowProvider.ShowDialog("Failed to find wafer center!");

                EditingDTO.WaferDTO.WaferMapDataDTO.WaferCircleCenter = FindWaferCenterByManuallyWindowViewModel.Cache.OffsetPosition;

                _isCircleCenterUpdatedInSession = true;
            }
            catch (Exception ex)
            {
                dialogWindowProvider.ShowDialog("Find Wafer Center failed! " + ex.Message, DialogButtonsEnum.OK, DialogIconEnum.Warning);
            }
            finally
            {
                NotifyAlignmentStatus();
            }
        }, cancellationToken);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task BrightFiledAlignmentAsync(CancellationToken cancellationToken)
    {
        Guard.IsNotNull(EditingDTO);

        try
        {
            AlignmentUserControlViewModel.IsDarkFieldAlignment = false;
            AlignmentUserControlViewModel.CalChipSiteModelEnum = CalChipSiteModelEnum.ChuckModel;
            await AlignmentUserControlViewModel.AlignmentAsync(cancellationToken).ConfigureAwait(false);

            EditingDTO.WaferDTO.AlignmentResultDto = AlignmentUserControlViewModel.AlignmentResult.Clone();

            _isAlignmentResultUpdatedInSession = true;
        }
        catch (Exception ex)
        {
            dialogWindowProvider.ShowDialog("Bright field alignment failed! " + ex.Message, DialogButtonsEnum.OK, DialogIconEnum.Warning);
        }
        finally
        {
            NotifyAlignmentStatus();
        }
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task DarkFiledAlignmentAsync(CancellationToken cancellationToken)
    {
        Guard.IsNotNull(EditingDTO);

        try
        {
            AlignmentUserControlViewModel.IsDarkFieldAlignment = true;
            AlignmentUserControlViewModel.CalChipSiteModelEnum = CalChipSiteModelEnum.ChuckModel;
            AlignmentUserControlViewModel.ProductivityInformation = Cache.ProductivityInformation;

            await AlignmentUserControlViewModel.AlignmentAsync(cancellationToken).ConfigureAwait(false);

            Cache.DarkFieldAlignmentDegree = AlignmentUserControlViewModel.AlignmentResult.Degrees;

            _isAlignmentResultUpdatedInSession = true;
        }
        catch (Exception ex)
        {
            dialogWindowProvider.ShowDialog("Bright field alignment failed! " + ex.Message, DialogButtonsEnum.OK, DialogIconEnum.Warning);
        }
        finally
        {
            NotifyAlignmentStatus();
        }
    }

    [RelayCommand]
    private void BrightFiledMarkSites()
    {
        alignmentWindowBrightFieldViewModel.Cache.CalChipSiteModelEnum = CalChipSiteModelEnum.ChuckModel;

        try
        {
            messenger.Send(ToggleToolsEventFactory.RefreshToolsWindowEnableStatus(false));

            Guard.IsTrue(windowManagerService.ShowDialog(alignmentWindowBrightFieldViewModel) == true, nameof(alignmentWindowBrightFieldViewModel));
        }
        finally
        {
            messenger.Send(ToggleToolsEventFactory.RefreshToolsWindowEnableStatus(true));
        }
    }

    [RelayCommand]
    private void DarkFiledMarkSites()
    {
        alignmentWindowDarkFieldViewModel.Cache.ProductivityInformation = Cache.ProductivityInformation;
        alignmentWindowDarkFieldViewModel.Cache.OpticsIlluminationModeEnum = Cache.ProductivityInformation.OpticsIlluminationModeEnum;

        try
        {
            messenger.Send(ToggleToolsEventFactory.RefreshToolsWindowEnableStatus(false));

            Guard.IsTrue(windowManagerService.ShowDialog(alignmentWindowDarkFieldViewModel) == true, nameof(alignmentWindowDarkFieldViewModel));
        }
        finally
        {
            messenger.Send(ToggleToolsEventFactory.RefreshToolsWindowEnableStatus(true));
        }
    }

    // 切换到 Alignment Tab 时调用，仅重新广播通知，不重新加载数据，为了解决TabControl延迟加载UI树的问题
    public void NotifyAll()
    {
        contextProvider.Post(() =>
        {
            OnPropertyChanged(nameof(AlignmentFindCenterCache));
            OnPropertyChanged(nameof(Cache));
            OnPropertyChanged(nameof(EditingDTO));
            OnPropertyChanged(nameof(FindWaferCenterByManuallyWindowViewModel));
        });
    }

    // 只要重做P8或P5，禁用waferMap编辑、禁用Build Wafer，直至P8和P5全部重新做完，则解除限制
    private void NotifyAlignmentStatus()
    {
        Guard.IsNotNull(EditingDTO);

        var isComplete = _isCircleCenterUpdatedInSession
                         && _isAlignmentResultUpdatedInSession
                         && EditingDTO.WaferDTO.IsAlignmentResultLegal();

        messenger.Send(ToggleRecipeEventFactory.UpdateIsWaferMapEditEnable(isComplete));

        messenger.Send(ToggleRecipeEventFactory.UpdateIsRecipeAlignment(isComplete));
        if (isComplete)
        {
            _isCircleCenterUpdatedInSession = false;
            _isAlignmentResultUpdatedInSession = false;
        }
    }

    public void Save()
    {
        try
        {
            recipeCacheProvider.Set(Cache, CancellationToken.None);
        }
        catch (Exception ex)
        {
            dialogWindowProvider.ShowDialog($"Save alignment cache failed! {ex.Message}",
                DialogButtonsEnum.OK,
                DialogIconEnum.Warning);
        }
    }
}

public sealed partial class Cache : ObservableCacheBase
{
    [ObservableProperty]
    private ProductivityInformation _productivityInformation = ProductivityInformation.Default;

    [ObservableProperty]
    private double _positionErrorThreshold;

    [ObservableProperty]
    private double _darkFieldAlignmentDegree;
}
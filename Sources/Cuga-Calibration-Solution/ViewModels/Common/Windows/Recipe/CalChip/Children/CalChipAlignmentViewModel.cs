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
using Core.Models.Models.Microscope.CalChip;
using Core.Recipe.Models;
using CugaCalibration.Core.Services.Interfaces;
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

namespace CugaCalibration.ViewModels.Common.Windows.Recipe.CalChip.Children;

[IOCAppService(ServiceType = typeof(CalChipAlignmentViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class CalChipAlignmentViewModel : ViewModelBase
{
    private bool _isLoaded;

    private readonly ICacheProvider _cacheProvider;
    private readonly ICacheProvider _recipeCacheProvider;
    private readonly IDialogWindowProvider _dialogWindowProvider;
    private readonly IWindowManagerService _windowManagerService;
    private readonly IMessenger _messenger;
    private readonly ISynchronizationContextProvider _contextProvider;
    private readonly IApplicationCookieService _applicationCookieService;
    private readonly AlignmentWindowBrightFieldViewModel _alignmentWindowBrightFieldViewModel;
    private readonly AlignmentWindowDarkFieldViewModel _alignmentWindowDarkFieldViewModel;
    private readonly StageViewModel _stageViewModel;
    private readonly ApplicationCookie _applicationCookie;

    /// <inheritdoc/>
    public CalChipAlignmentViewModel(ICacheProvider cacheProvider,
        [FromKeyedServices(CalibrationConstantsHelper.RecipeDbKey)]
        ICacheProvider recipeCacheProvider,
        IDialogWindowProvider dialogWindowProvider,
        IWindowManagerService windowManagerService,
        IMessenger messenger,
        ISynchronizationContextProvider contextProvider,
        IApplicationCookieService applicationCookieService,
        AlignmentWindowBrightFieldViewModel alignmentWindowBrightFieldViewModel,
        AlignmentWindowDarkFieldViewModel alignmentWindowDarkFieldViewModel,
        StageViewModel stageViewModel,
        ApplicationCookie applicationCookie)
    {
        _cacheProvider = cacheProvider;
        _recipeCacheProvider = recipeCacheProvider;
        _dialogWindowProvider = dialogWindowProvider;
        _windowManagerService = windowManagerService;
        _messenger = messenger;
        _contextProvider = contextProvider;
        _applicationCookieService = applicationCookieService;
        _alignmentWindowBrightFieldViewModel = alignmentWindowBrightFieldViewModel;
        _alignmentWindowDarkFieldViewModel = alignmentWindowDarkFieldViewModel;
        _stageViewModel = stageViewModel;
        _applicationCookie = applicationCookie;
    }

    public ApplicationCookie ApplicationCookie => _applicationCookie;

    private CalChipRecipeDTO? _editDTO;

    /// <summary>
    /// 当前编辑中的 CalChip 配方项
    /// </summary>
    public CalChipRecipeDTOItem? EditingItem { get; private set; }

    [ObservableProperty]
    public partial CalChipSiteModelEnum CurrentCalChipSiteModelEnum { get; set; }

    [ObservableProperty]
    public partial AlignmentUserControlViewModel AlignmentUserControlViewModel { get; set; } = HostApplication.GetRequiredService<AlignmentUserControlViewModel>();

    [ObservableProperty]
    public partial AlignmentCacheBrightField AlignmentCacheBrightField { get; set; } = new();

    [ObservableProperty]
    public partial AlignmentCacheBrightField[] AlignmentCacheBrightFields { get; set; } = [];

    [ObservableProperty]
    public partial AlignmentCacheDarkField AlignmentCacheDarkField { get; set; } = new();

    [ObservableProperty]
    public partial AlignmentCacheDarkField[] AlignmentCacheDarkFields { get; set; } = [];

    [ObservableProperty]
    public partial Cache Cache { get; set; } = new();

    [ObservableProperty]
    public partial MicroscopeCalChipDTO MicroscopeCalChip { get; set; } = new();

    public void Initialize(CalChipRecipeDTO calChipRecipeDTO)
    {
        _editDTO = calChipRecipeDTO;
        EditingItem = _editDTO.CurrentItem;
        CurrentCalChipSiteModelEnum = _editDTO.CalChipSiteModelEnum;

        // 对准缓存与普通 Recipe 共享，根据 CalChipSiteModelEnum 查找
        if (_isLoaded == false)
        {
            MicroscopeCalChip = _applicationCookieService.GetCalibration<MicroscopeCalChipDTO>();
            AlignmentCacheBrightFields = _cacheProvider.GetOrDefaultArray<AlignmentCacheBrightField>();
            Cache = _cacheProvider.GetOrDefault<Cache>();
        }

        MicroscopeCalChip.CalChipSiteModelEnum = CurrentCalChipSiteModelEnum;

        AlignmentCacheBrightField = AlignmentCacheBrightFields
            .SingleOrDefault(t => t.CalChipSiteModelEnum == CurrentCalChipSiteModelEnum) ?? new AlignmentCacheBrightField
            {
                CalChipSiteModelEnum = CurrentCalChipSiteModelEnum
            };
        AlignmentUserControlViewModel.AlignmentCacheBrightField = AlignmentCacheBrightField.Clone();

        RefreshDarkFieldCache();

        _isLoaded = true;
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task BrightFiledAlignmentAsync(CancellationToken cancellationToken)
    {
        Guard.IsNotNull(EditingItem);

        try
        {
            AlignmentUserControlViewModel.IsDarkFieldAlignment = false;
            AlignmentUserControlViewModel.CalChipSiteModelEnum = CurrentCalChipSiteModelEnum;
            await AlignmentUserControlViewModel.AlignmentAsync(cancellationToken).ConfigureAwait(false);
            EditingItem.CalChipMapDTO.AlignmentResultDto = AlignmentUserControlViewModel.AlignmentResult.Clone();

            EditingItem.AlignmentAbsoluteAngle = _stageViewModel.GetMachineStageTheta();

            EditingItem.CalChipMapDTO.WaferMapDataDTO.WaferCircleCenter = _stageViewModel.MachineToBrightFieldPosition(MicroscopeCalChip.CurrentItem.BrightFieldMachinePosition).DegreeAngleByXy(EditingItem.AlignmentAbsoluteAngle.Value);
        }
        catch (Exception ex)
        {
            _dialogWindowProvider.ShowDialog("Bright field alignment failed! " + ex.Message, DialogButtonsEnum.OK, DialogIconEnum.Warning);
        }
        finally
        {
            NotifyAlignmentStatus();
        }
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task DarkFiledAlignmentAsync(CancellationToken cancellationToken)
    {
        Guard.IsNotNull(EditingItem);

        try
        {
            AlignmentUserControlViewModel.IsDarkFieldAlignment = true;
            AlignmentUserControlViewModel.CalChipSiteModelEnum = CurrentCalChipSiteModelEnum;
            AlignmentUserControlViewModel.ProductivityInformation = Cache.ProductivityInformation;
            await AlignmentUserControlViewModel.AlignmentAsync(cancellationToken).ConfigureAwait(false);

            Cache.DarkFieldAlignmentDegree = AlignmentUserControlViewModel.AlignmentResult.Degrees;
            RefreshDarkFieldCache();
        }
        catch (Exception ex)
        {
            _dialogWindowProvider.ShowDialog("Dark field alignment failed! " + ex.Message, DialogButtonsEnum.OK, DialogIconEnum.Warning);
        }
        finally
        {
            NotifyAll();
        }
    }

    [RelayCommand]
    private void BrightFiledMarkSites()
    {
        AlignmentUserControlViewModel.IsDarkFieldAlignment = false;
        _alignmentWindowBrightFieldViewModel.Cache.CalChipSiteModelEnum = CurrentCalChipSiteModelEnum;

        try
        {
            _messenger.Send(ToggleToolsEventFactory.RefreshToolsWindowEnableStatus(false));

            Guard.IsTrue(_windowManagerService.ShowDialog(_alignmentWindowBrightFieldViewModel) == true, nameof(_alignmentWindowBrightFieldViewModel));
        }
        finally
        {
            _messenger.Send(ToggleToolsEventFactory.RefreshToolsWindowEnableStatus(true));
        }

        AlignmentCacheBrightFields = _cacheProvider.GetOrDefaultArray<AlignmentCacheBrightField>();
        AlignmentCacheBrightField = AlignmentCacheBrightFields.SingleOrDefault(t => t.CalChipSiteModelEnum == CurrentCalChipSiteModelEnum
            , new AlignmentCacheBrightField { CalChipSiteModelEnum = CurrentCalChipSiteModelEnum });
        AlignmentUserControlViewModel.AlignmentCacheBrightField = AlignmentCacheBrightField.Clone();
    }

    [RelayCommand]
    private void DarkFiledMarkSites()
    {
        AlignmentUserControlViewModel.IsDarkFieldAlignment = true;
        _alignmentWindowDarkFieldViewModel.Cache.ProductivityInformation = Cache.ProductivityInformation;
        _alignmentWindowDarkFieldViewModel.Cache.CalChipSiteModelEnum = CurrentCalChipSiteModelEnum;

        try
        {
            _messenger.Send(ToggleToolsEventFactory.RefreshToolsWindowEnableStatus(false));
            _messenger.Send(ToggleToolsEventFactory.RefreshProductivityEnableStatus(true));

            Guard.IsTrue(_windowManagerService.ShowDialog(_alignmentWindowDarkFieldViewModel) == true, nameof(_alignmentWindowDarkFieldViewModel));
        }
        finally
        {
            _messenger.Send(ToggleToolsEventFactory.RefreshToolsWindowEnableStatus(true));
            _messenger.Send(ToggleToolsEventFactory.RefreshProductivityEnableStatus(true));
        }

        Cache.ProductivityInformation = _alignmentWindowDarkFieldViewModel.Cache.ProductivityInformation;
        RefreshDarkFieldCache();
    }

    public void NotifyAll()
    {
        _contextProvider.Post(() =>
        {
            OnPropertyChanged(nameof(AlignmentCacheBrightField));
            OnPropertyChanged(nameof(AlignmentCacheDarkField));
            OnPropertyChanged(nameof(Cache));
            OnPropertyChanged(nameof(EditingItem));
        });
    }

    private void NotifyAlignmentStatus()
    {
        Guard.IsNotNull(EditingItem);

        _messenger.Send(ToggleCalChipRecipeEventFactory.UpdateIsWaferMapEditEnable(true));

        _messenger.Send(ToggleCalChipRecipeEventFactory.UpdateIsCalChipRecipeAlignment(EditingItem.AlignmentAbsoluteAngle != null));
    }

    public void Save()
    {
        try
        {
            _cacheProvider.Set(Cache, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _dialogWindowProvider.ShowDialog($"Save alignment cache failed! {ex.Message}",
                DialogButtonsEnum.OK,
                DialogIconEnum.Warning);
        }
    }

    public void Dispose()
    {
        _isLoaded = false;
    }

    private void RefreshDarkFieldCache()
    {
        AlignmentCacheDarkFields =
        [
            .. _recipeCacheProvider.GetOrDefaultArray<AlignmentCacheDarkField>(),
            .. _cacheProvider.GetOrDefaultArray<AlignmentCacheDarkField>()
        ];
        AlignmentCacheDarkField = AlignmentCacheDarkFields.SingleOrDefault(t =>
                t.ProductivityInformation == Cache.ProductivityInformation
                && t.CalChipSiteModelEnum == CurrentCalChipSiteModelEnum
            , new AlignmentCacheDarkField
            {
                CalChipSiteModelEnum = CurrentCalChipSiteModelEnum,
                ProductivityInformation = Cache.ProductivityInformation
            });
        AlignmentUserControlViewModel.AlignmentCacheDarkField = AlignmentCacheDarkField.Clone();
    }
}

public sealed partial class Cache : ObservableCacheBase
{
    [ObservableProperty]
    public partial ProductivityInformation ProductivityInformation { get; set; } = ProductivityInformation.Default;

    [ObservableProperty]
    public partial double DarkFieldAlignmentDegree { get; set; }
}
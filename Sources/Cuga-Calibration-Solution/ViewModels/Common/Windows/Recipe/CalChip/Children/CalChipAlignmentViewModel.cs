using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Core.Models.Enums.Stage;
using Core.Models.Events;
using Core.Models.Models.Common.Alignment;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Microscope.CalChip;
using Core.Recipe.Models;
using CugaCalibration.Core.Services.Interfaces;
using CugaCalibration.ViewModels.Common.Windows.Tools.Alignment;
using Local.SQL.Cache.Providers.Services.Interfaces;
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
    private readonly IDialogWindowProvider _dialogWindowProvider;
    private readonly IWindowManagerService _windowManagerService;
    private readonly IMessenger _messenger;
    private readonly ISynchronizationContextProvider _contextProvider;
    private readonly ICalibrationStatusService _calibrationStatusService;
    private readonly IApplicationCookieService _applicationCookieService;
    private readonly AlignmentWindowBrightFieldViewModel _alignmentWindowBrightFieldViewModel;
    private readonly StageViewModel _stageViewModel;
    private readonly ApplicationCookie _applicationCookie;

    /// <inheritdoc/>
    public CalChipAlignmentViewModel(ICacheProvider cacheProvider,
        IDialogWindowProvider dialogWindowProvider,
        IWindowManagerService windowManagerService,
        IMessenger messenger,
        ISynchronizationContextProvider contextProvider,
        ICalibrationStatusService calibrationStatusService,
        IApplicationCookieService applicationCookieService,
        AlignmentWindowBrightFieldViewModel alignmentWindowBrightFieldViewModel,
        StageViewModel stageViewModel,
        ApplicationCookie applicationCookie)
    {
        _cacheProvider = cacheProvider;
        _dialogWindowProvider = dialogWindowProvider;
        _windowManagerService = windowManagerService;
        _messenger = messenger;
        _contextProvider = contextProvider;
        _calibrationStatusService = calibrationStatusService;
        _applicationCookieService = applicationCookieService;
        _alignmentWindowBrightFieldViewModel = alignmentWindowBrightFieldViewModel;
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
        }

        MicroscopeCalChip.CalChipSiteModelEnum = CurrentCalChipSiteModelEnum;

        AlignmentCacheBrightField = AlignmentCacheBrightFields
            .SingleOrDefault(t => t.CalChipSiteModelEnum == CurrentCalChipSiteModelEnum) ?? new AlignmentCacheBrightField
        {
            CalChipSiteModelEnum = CurrentCalChipSiteModelEnum
        };
        AlignmentUserControlViewModel.AlignmentCacheBrightField = AlignmentCacheBrightField.Clone();

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

    [RelayCommand]
    private void BrightFiledMarkSites()
    {
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
    }

    public void NotifyAll()
    {
        _contextProvider.Post(() =>
        {
            OnPropertyChanged(nameof(AlignmentCacheBrightField));
            OnPropertyChanged(nameof(EditingItem));
        });
    }

    private void NotifyAlignmentStatus()
    {
        Guard.IsNotNull(EditingItem);

        _messenger.Send(ToggleCalChipRecipeEventFactory.UpdateIsWaferMapEditEnable(true));

        _messenger.Send(ToggleCalChipRecipeEventFactory.UpdateIsCalChipRecipeAlignment(EditingItem.AlignmentAbsoluteAngle != null));
    }

    public void Dispose()
    {
        _isLoaded = false;
    }
}
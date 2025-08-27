using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Core.Models.Events;
using Core.Models.Models;
using Core.Models.Models.Common.Alignment;
using Core.Models.Models.Common.Recipe;
using CugaCalibration.ViewModels.Common.Windows.Tools;
using CugaCalibration.ViewModels.Common.Windows.Tools.Alignment;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.IOC.Providers;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM.Providers;
using Point = Net.Utilities.Models.Geometries.Point;

namespace CugaCalibration.ViewModels.Common.Windows.Management.Recipe;

[IOCAppService(ServiceType = typeof(RecipeRequireActionViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class RecipeRequireActionViewModel : CalibrationViewModelBase, IRecipient<ValueChangedMessage<ToggleCalibrateEvent>>
{
    private readonly ISynchronizationContextProvider _contextProvider;
    private readonly IDialogWindowProvider _dialogWindowProvider;
    private readonly RecipeSettingViewModel _recipeSettingViewModel;
    private readonly AlignmentWindowBrightFieldViewModel _alignmentWindowBrightFieldViewModel;

    #region 属性

    public FindWaferCenterByManuallyWindowViewModel FindWaferCenterByManuallyWindowViewModel { get; }

    public override List<CalibrationItemStep> CalibrationStepList { get; } =
    [
        new() { StepName = "Find Wafer Bright Field Center offset" },
        new() { StepName = "Alignment" },
    ];

    #region 缓存

    [ObservableProperty]
    private AlignmentCacheBrightField _alignmentCacheBrightField = new();

    [ObservableProperty]
    private AlignmentFindCenterCache _alignmentFindCenterCache = new();

    #endregion 缓存

    #region 界面

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RequireActionCommandCommand))]
    private bool _isCalibrateEnable;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CancelCommandCommand))]
    private bool _isCancelEnable;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PreviousCommandCommand))]
    private bool _isPreviousEnable;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(NextCommandCommand))]
    private bool _isNextEnable;

    [ObservableProperty]
    private bool _isActionFinished;

    #endregion 界面

    #region 结果

    /// <summary>
    /// wafer中心晶圆坐标
    /// </summary>
    [ObservableProperty]
    private Point? _findWaferCenterOffset;

    /// <summary>
    /// 对准结果
    /// </summary>
    [ObservableProperty]
    private AlignmentResultDto? _alignmentResult;

    #endregion 结果

    #endregion 属性

    public RecipeRequireActionViewModel(
        ISynchronizationContextProvider contextProvider,
        IDialogWindowProvider dialogWindowProvider,
        RecipeSettingViewModel recipeSettingViewModel,
        AlignmentWindowBrightFieldViewModel alignmentWindowBrightFieldViewModel,
        FindWaferCenterByManuallyWindowViewModel findWaferCenterByManuallyWindowViewModel)
    {
        _dialogWindowProvider = dialogWindowProvider;
        _recipeSettingViewModel = recipeSettingViewModel;
        _alignmentWindowBrightFieldViewModel = alignmentWindowBrightFieldViewModel;
        _contextProvider = contextProvider;
        FindWaferCenterByManuallyWindowViewModel = findWaferCenterByManuallyWindowViewModel;
        Messenger.UnregisterAll(this);
        Messenger.RegisterAll(this);
    }

    #region 控制校准业务重载

    [RelayCommand]
    private async Task LoadedCommandAsync()
    {
        await LoadedAsync().ConfigureAwait(false);
        _contextProvider.Post(() => { IsCalibrateEnable = true; });
    }

    [RelayCommand(CanExecute = nameof(IsCalibrateEnable))]
    private async Task RequireActionCommandAsync()
    {
        IsRecipeEditing = true;
        await CalibrateAsync().ConfigureAwait(false);
        if (FindWaferCenterOffset is not null)
        {
            Messenger.Send(ToggleCalibrateEventFactory.UpdateIsNextEnable(CalibrationStepIndex == 0));
        }
    }

    [RelayCommand(CanExecute = nameof(IsNextEnable))]
    private async Task NextCommandAsync() => await NextAsync().ConfigureAwait(false);

    [RelayCommand(CanExecute = nameof(IsPreviousEnable))]
    private async Task PreviousCommandAsync()
    {
        await PreviousAsync().ConfigureAwait(false);
        if (FindWaferCenterOffset is not null)
        {
            Messenger.Send(ToggleCalibrateEventFactory.UpdateIsNextEnable(CalibrationStepIndex == 0));
        }
    }

    [RelayCommand(CanExecute = nameof(IsCancelEnable))]
    private async Task CancelCommandAsync() => await CancelAsync().ConfigureAwait(false);

    protected override async Task<bool> LoadedingAsync(CancellationToken cancellationToken)
    {
        var calibrationRecipeDto = RecipeCacheProvider.GetOrDefault<CalibrationRecipeDto>();
        FindWaferCenterOffset = calibrationRecipeDto.WaferDto.WaferCenterWaferPosition;
        AlignmentResult = calibrationRecipeDto.WaferDto.AlignmentResultDto;

        AlignmentCacheBrightField = RecipeCacheProvider.GetOrDefault<AlignmentCacheBrightField>();

        await FindWaferCenterByManuallyWindowViewModel.LoadedAsync().ConfigureAwait(false);
        AlignmentFindCenterCache = FindWaferCenterByManuallyWindowViewModel.Cache;
        FindWaferCenterOffset = AlignmentFindCenterCache.OffsetPosition;
        return true;
    }

    protected override Task<bool> NextingAsync(CancellationToken cancellationToken)
    {
        IsCalibrated = CalibrationStepIndex == CalibrationStepList.Count - 1;
        return Task.FromResult(true);
    }

    protected override Task<bool> CancelingAsync()
    {
        if (IsOk())
        {
            _recipeSettingViewModel.CalibrationRecipeDto.WaferDto.WaferCenterWaferPosition = new Point(FindWaferCenterOffset!.Value.X, FindWaferCenterOffset!.Value.Y);
            _recipeSettingViewModel.CalibrationRecipeDto.WaferDto.AlignmentResultDto = AlignmentResult!.Clone();

            _recipeSettingViewModel.IsEditWaferMapEnable = true;

            _dialogWindowProvider.ShowDialog("Require action finished! Please redo wafer map", DialogButtonsEnum.OK, DialogIconEnum.Warning);
        }

        IsRecipeEditing = false;

        return Task.FromResult(true);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step0CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(async () =>
        {
            var result = await FindWaferCenterByManuallyWindowViewModel.ActionAsync(cancellationToken).ConfigureAwait(false);
            FindWaferCenterOffset = FindWaferCenterByManuallyWindowViewModel.Cache.OffsetPosition;
            return result;
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private Task Step1CalibrateActionAsync(CancellationToken cancellationToken)
    {
        return InvokeCalibrateAsync(() =>
        {
            AlignmentResult = StageViewModel.Alignment(AlignmentCacheBrightField.LowSite1, AlignmentCacheBrightField.LowSite2,
                AlignmentCacheBrightField.HighSite1, AlignmentCacheBrightField.HighSite2,
                AlignmentCacheBrightField.LowMag, AlignmentCacheBrightField.HighMag,
                AlignmentCacheBrightField.AlgorithmWaferTypeEnum);
            return true;
        });
    }

    [RelayCommand]
    private void SelectSites()
    {
        var showDialog = WindowManagerService.ShowDialog(_alignmentWindowBrightFieldViewModel);
        if (showDialog == false)
        {
            DialogWindowProvider.ShowDialog("Alignment Setting is Empty", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return;
        }

        AlignmentCacheBrightField = RecipeCacheProvider.GetOrDefault<AlignmentCacheBrightField>();
    }

    public bool IsOk()
    {
        return FindWaferCenterOffset != null
               && AlignmentResult != null;
    }

    public void Receive(ValueChangedMessage<ToggleCalibrateEvent> message)
    {
        _contextProvider.Post(() =>
        {
            if (message.Value.IsCalibrateEnable.HasValue)
                IsCalibrateEnable = message.Value.IsCalibrateEnable.Value;
            if (message.Value.IsCancelEnable.HasValue)
                IsCancelEnable = message.Value.IsCancelEnable.Value;
            if (message.Value.IsPreviousEnable.HasValue)
                IsPreviousEnable = message.Value.IsPreviousEnable.Value;
            if (message.Value.IsNextEnable.HasValue)
                IsNextEnable = message.Value.IsNextEnable.Value;
        });
    }

    #endregion 控制校准业务重载
}
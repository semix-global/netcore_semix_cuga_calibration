using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Core.Models.Enums.Stage;
using Core.Models.Events;
using Core.Models.Models.Setting;
using Core.Recipe.Models.Wafer;
using CugaCalibration.Core.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Graphics.Primitives.Enums.Editors;
using Net.Utilities.Graphics.Primitives.ObjectModels;
using Net.Utilities.IOC.Providers;
using Net.Utilities.Models.Geometries;
using Net.Utilities.WaferMap.WPF;
using Net.Utilities.WaferMap.WPF.Documents;
using Net.Utilities.WaferMap.WPF.Drawables;
using Net.Utilities.WaferMap.WPF.Editors;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;

namespace CugaCalibration.ViewModels.Common.Windows.Recipe.Edit.Children;

[IOCAppService(ServiceType = typeof(RecipeWaferSettingUserControlViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Transient)]
public sealed partial class RecipeWaferSettingUserControlViewModel : ViewModelBase, IDisposable, IRecipient<ValueChangedMessage<ToggleWaferMapEvent>>
{
    private readonly ILogger<RecipeWaferSettingUserControlViewModel> _logger;
    private readonly IDialogWindowProvider _dialogWindowProvider;
    private readonly ICalibrationRecipeService _calibrationRecipeService;
    private readonly ISynchronizationContextProvider _contextProvider;
    private readonly StageViewModel _stageViewModel;
    private readonly MicroscopeViewModel _microscopeViewModel;
    private readonly CalibrationSetting _calibrationSetting;

    private CancellationTokenSource? _cancellationTokenSource;

    #region 属性

    #region 界面属性

    [ObservableProperty]
    public partial WaferMapCanvasDocument WaferMapCanvasDocument { get; set; } = new();

    [ObservableProperty]
    public partial WaferMapCanvasViewModel WaferMapCanvasViewModel { get; private set; } = new();

    [ObservableProperty]
    public partial WaferMapDieSelectionInputOptions WaferMapDieSelectionInputOptions { get; private set; } = new();

    public SelectionSet<WaferMapDie>? SelectionDies { get; private set; }

    public double WaferRadius
    {
        get => WaferMapCanvasViewModel.Document.WaferBuilder.Circle.Radius;
        set
        {
            Lock();
            WaferMapCanvasViewModel.Document.WaferBuilder.Circle = new Circle(WaferDTO.WaferMapDataDTO.WaferCircleCenter, value);
            RefreshWaferMapDataSource();
            OnPropertyChanged();
        }
    }

    public double WaferDiePitchSizeWidth
    {
        get => WaferMapCanvasViewModel.Document.DieBuilder.DiePitchSize.Width;
        set
        {
            Lock();
            WaferMapCanvasViewModel.Document.DieBuilder.DiePitchSize = new Size(value, WaferMapCanvasViewModel.Document.DieBuilder.DiePitchSize.Height);
            RefreshWaferMapDataSource();
            OnPropertyChanged();
        }
    }

    public double WaferDiePitchSizeHeight
    {
        get => WaferMapCanvasViewModel.Document.DieBuilder.DiePitchSize.Height;
        set
        {
            Lock();
            WaferMapCanvasViewModel.Document.DieBuilder.DiePitchSize = new Size(WaferMapCanvasViewModel.Document.DieBuilder.DiePitchSize.Width, value);
            RefreshWaferMapDataSource();
            OnPropertyChanged();
        }
    }

    public double WaferDieScribeSizeWidth
    {
        get => WaferMapCanvasViewModel.Document.DieBuilder.DieScribeSize.Width;
        set
        {
            Lock();
            WaferMapCanvasViewModel.Document.DieBuilder.DieScribeSize = new Size(value, WaferMapCanvasViewModel.Document.DieBuilder.DieScribeSize.Height);
            RefreshWaferMapDataSource();
            OnPropertyChanged();
        }
    }

    public double WaferDieScribeSizeHeight
    {
        get => WaferMapCanvasViewModel.Document.DieBuilder.DieScribeSize.Height;
        set
        {
            Lock();
            WaferMapCanvasViewModel.Document.DieBuilder.DieScribeSize = new Size(WaferMapCanvasViewModel.Document.DieBuilder.DieScribeSize.Width, value);
            RefreshWaferMapDataSource();
            OnPropertyChanged();
        }
    }

    public double WaferReticleDiePitchSizeWidth
    {
        get => WaferMapCanvasViewModel.Document.ReticleBuilder.DiePitchSize.Width;
        set
        {
            Lock();
            WaferMapCanvasViewModel.Document.ReticleBuilder.DiePitchSize = new Size(value, WaferMapCanvasViewModel.Document.ReticleBuilder.DiePitchSize.Height);
            RefreshWaferMapDataSource();
            OnPropertyChanged();
        }
    }

    public double WaferReticlePitchDieSizeHeight
    {
        get => WaferMapCanvasViewModel.Document.ReticleBuilder.DiePitchSize.Height;
        set
        {
            Lock();
            WaferMapCanvasViewModel.Document.ReticleBuilder.DiePitchSize = new Size(WaferMapCanvasViewModel.Document.ReticleBuilder.DiePitchSize.Width, value);
            RefreshWaferMapDataSource();
            OnPropertyChanged();
        }
    }

    public double WaferReticleDieScribeSizeWidth
    {
        get => WaferMapCanvasViewModel.Document.ReticleBuilder.DieScribeSize.Width;
        set
        {
            Lock();
            WaferMapCanvasViewModel.Document.ReticleBuilder.DieScribeSize = new Size(value, WaferMapCanvasViewModel.Document.ReticleBuilder.DieScribeSize.Height);
            RefreshWaferMapDataSource();
            OnPropertyChanged();
        }
    }

    public double WaferReticleDieScribeSizeHeight
    {
        get => WaferMapCanvasViewModel.Document.ReticleBuilder.DieScribeSize.Height;
        set
        {
            Lock();
            WaferMapCanvasViewModel.Document.ReticleBuilder.DieScribeSize = new Size(WaferMapCanvasViewModel.Document.ReticleBuilder.DieScribeSize.Width, value);
            RefreshWaferMapDataSource();
            OnPropertyChanged();
        }
    }

    public int WaferReticleDieCountX
    {
        get => WaferMapCanvasViewModel.Document.ReticleBuilder.ReticleDieCount.XCount;
        set
        {
            Lock();
            WaferMapCanvasViewModel.Document.ReticleBuilder.ReticleDieCount = WaferMapCanvasViewModel.Document.ReticleBuilder.ReticleDieCount with { XCount = value };
            RefreshWaferMapDataSource();
            OnPropertyChanged();
        }
    }

    public int WaferReticleDieCountY
    {
        get => WaferMapCanvasViewModel.Document.ReticleBuilder.ReticleDieCount.YCount;
        set
        {
            Lock();
            WaferMapCanvasViewModel.Document.ReticleBuilder.ReticleDieCount = WaferMapCanvasViewModel.Document.ReticleBuilder.ReticleDieCount with { YCount = value };
            RefreshWaferMapDataSource();
            OnPropertyChanged();
        }
    }

    #endregion

    [ObservableProperty]
    public partial Point StageDirection { get; set; }

    [ObservableProperty]
    public partial bool IsAlignmentOk { get; set; }

    [ObservableProperty]
    public partial WaferDTO WaferDTO { get; set; } = new();

    [ObservableProperty]
    public partial CalChipSiteModelEnum CalChipSiteModelEnum { get; set; }

    /// <inheritdoc/>
    public RecipeWaferSettingUserControlViewModel(ILogger<RecipeWaferSettingUserControlViewModel> logger,
        IDialogWindowProvider dialogWindowProvider,
        ICalibrationRecipeService calibrationRecipeService,
        ISynchronizationContextProvider contextProvider,
        IMessenger messenger,
        StageViewModel stageViewModel,
        MicroscopeViewModel microscopeViewModel,
        CalibrationSetting calibrationSetting)
    {
        _logger = logger;
        _dialogWindowProvider = dialogWindowProvider;
        _calibrationRecipeService = calibrationRecipeService;
        _contextProvider = contextProvider;
        _stageViewModel = stageViewModel;
        _microscopeViewModel = microscopeViewModel;
        _calibrationSetting = calibrationSetting;

        messenger.UnregisterAll(this);
        messenger.RegisterAll(this);

        Initialize();
    }

    #endregion


    private void Initialize()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = new CancellationTokenSource();

        var token = _cancellationTokenSource.Token;

        if (StageDirection == Point.Origin)
        {
            var (xDirection, yDirection) = _stageViewModel.GetMachineDirection();
            StageDirection = new Point(xDirection, yDirection);
        }

        WaferMapCanvasViewModel.Document.Settings.IsShowToggleControlOfIsShowAxes = true;
        WaferMapCanvasViewModel.Document.Settings.IsShowToggleControlOfIsShowCursor = true;
        WaferMapCanvasViewModel.Document.Settings.IsShowToggleControlOfIsShowGrid = true;

        WaferMapCanvasViewModel.Document = WaferMapCanvasDocument;

        // Die 选择后台任务: 循环等待用户在 WaferMap 上点选 Die, 供 Goto Position 使用
        _ = Task.Run(async () =>
        {
            try
            {
                WaferMapCanvasViewModel.IsToggleSelection = false;
                WaferMapDieSelectionInputOptions.IsMultipleSelection = false;
                WaferMapDieSelectionInputOptions.CancellationToken = token;
                WaferMapDieSelectionInputOptions.Initialize();

                while (token.IsCancellationRequested == false)
                {
                    var outputResult = await WaferMapDieSelectionGetter
                        .RunAsync<WaferMapDieSelectionGetter>(WaferMapCanvasViewModel.Document.Edit, WaferMapDieSelectionInputOptions);

                    if (outputResult.Output.Count > 1) continue;
                    if (outputResult.OutputResultModeEnum == OutputResultModeEnum.Ok)
                    {
                        foreach (var die in outputResult.Output)
                            die.IsSelected = true;
                    }

                    SelectionDies = outputResult.Output;
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Task monitor error!");
            }
        }, token);
    }


    #region Command

    [RelayCommand]
    private async Task RefreshWaferMapAsync()
    {
        await Task.Run(async () =>
        {
            try
            {
                if (!IsAlignmentOk)
                {
                    _dialogWindowProvider.ShowDialog(
                        "The wafer map can only be refreshed after the alignment is successful. Please go to the Alignment page to action.",
                        DialogButtonsEnum.OK,
                        DialogIconEnum.Warning);
                    return;
                }

                var currentLens = _microscopeViewModel.GetCurrentMicroscopeLensInformation();
                var configHighLens = _calibrationSetting.SettingCommonParam.HighMicroscopeLensInformation;

                if (currentLens != configHighLens)
                {
                    _dialogWindowProvider.ShowDialog(
                        $"The generation wafermap must to be done under a {configHighLens.LensName}. Please re-obtain the origin die coordinates",
                        DialogButtonsEnum.OK,
                        DialogIconEnum.Warning);
                    await _microscopeViewModel.SwitchMicroscopeLensInformationAsync(configHighLens, cancellationToken: CancellationToken.None).ConfigureAwait(false);
                    return;
                }

                var originDieWaferPosition = _stageViewModel.GetBrightFieldStagePosition();

                Lock();
                WaferMapCanvasDocument.DieBuilder.OriginalDiePoint = originDieWaferPosition;
                WaferMapCanvasDocument.ReticleBuilder.OriginalDiePoint = originDieWaferPosition;
                _contextProvider.Send(() =>
                {
                    WaferMapCanvasViewModel.Document.OverlayCrossLine.Point = WaferMapCanvasViewModel.Document.DieBuilder.OriginalDiePoint;
                    WaferMapCanvasViewModel.Document.OverlayCrossLine.IsVisible = true;
                });

                RefreshWaferMapDataSource();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Refresh Wafer Map Failed");
                _dialogWindowProvider.ShowDialog($"Refresh Wafer Map Failed! {ex.Message}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            }
        });
    }

    [RelayCommand]
    private async Task GotoPositionAsync()
    {
        await Task.Run(() =>
        {
            try
            {
                if (SelectionDies is null)
                {
                    _logger.LogWarning("SelectionDies is null");
                    return;
                }

                if (SelectionDies.Count == 0)
                {
                    _logger.LogWarning("No die selected");
                    return;
                }

                var waferMapDie = SelectionDies.ElementAt(0);

                _calibrationRecipeService.GetWaferMapDieMachinePosition(WaferDTO, waferMapDie, out var machinePosition);

                _stageViewModel.SetCalChipBrightFieldAbsoluteStageXy(_stageViewModel.MachineToBrightFieldPosition(machinePosition), CalChipSiteModelEnum);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Goto Position failed");
                _dialogWindowProvider.ShowDialog($"Goto Position failed: {ex.Message}", DialogButtonsEnum.OK, DialogIconEnum.Error);
            }
        }).ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task ReviewAsync()
    {
        await Task.Run(async () =>
        {
            try
            {
                await _calibrationRecipeService.GetCorrectWaferMapByOffsetAsync(WaferDTO, true, CalChipSiteModelEnum).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Update wafer map failed");
                _dialogWindowProvider.ShowDialog($"Update wafer map failed: {ex.Message}", DialogButtonsEnum.OK, DialogIconEnum.Error);
            }
        }).ConfigureAwait(false);
    }

    #endregion

    public void CancelToken()
    {
        try
        {
            _cancellationTokenSource?.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cancel Token Failed!");
        }
    }

    /// <summary>
    /// 释放资源，停止所有后台任务
    /// </summary>
    public void Dispose()
    {
        CancelToken();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
    }

    public void Receive(ValueChangedMessage<ToggleWaferMapEvent> message)
    {
        _contextProvider.Post(() =>
        {
            if (message.Value.IsRefreshWaferMap.HasValue)
            {
                if (message.Value.IsRefreshWaferMap.Value)
                {
                    RefreshWaferMapCanvasDocument();
                    NotifyWaferParametersChanged();

                    WaferMapCanvasViewModel.Document.OverlayCrossLine.Point = WaferMapCanvasViewModel.Document.DieBuilder.OriginalDiePoint;
                    WaferMapCanvasViewModel.Document.OverlayCrossLine.IsVisible = true;
                    WaferMapCanvasViewModel.Document.Refresh();
                }
            }
        });
    }

    #region 刷新

    /// <summary>
    /// 通知WaferMap绘图刷新
    /// </summary>
    private void RefreshWaferMapCanvasDocument()
    {
        using var scope = WaferMapCanvasDocument.View.Sync.EnterScope();

        WaferMapCanvasDocument.WaferBuilder.Circle = new Circle(WaferDTO.WaferMapDataDTO.WaferCircleCenter, WaferDTO.WaferMapDataDTO.WaferDiameter / 2d);
        WaferMapCanvasDocument.DieBuilder.OriginalDiePoint = WaferDTO.WaferMapDataDTO.WaferOriginalDiePoint;
        WaferMapCanvasDocument.DieBuilder.DiePitchSize = new Size(WaferDTO.WaferMapDataDTO.CellDieWidth, WaferDTO.WaferMapDataDTO.CellDieHeight);
        WaferMapCanvasDocument.DieBuilder.DieScribeSize = new Size(WaferDTO.WaferMapDataDTO.DieScribeWidth, WaferDTO.WaferMapDataDTO.DieScribeHeight);

        WaferMapCanvasDocument.ReticleBuilder.OriginalDiePoint = WaferDTO.WaferMapDataDTO.WaferReticleOriginalDiePoint;
        WaferMapCanvasDocument.ReticleBuilder.DiePitchSize = new Size(WaferDTO.WaferMapDataDTO.ReticleWidth, WaferDTO.WaferMapDataDTO.ReticleHeight);
        WaferMapCanvasDocument.ReticleBuilder.DieScribeSize = new Size(WaferDTO.WaferMapDataDTO.ReticleScribeWidth, WaferDTO.WaferMapDataDTO.ReticleScribeHeight);
        WaferMapCanvasDocument.ReticleBuilder.ReticleDieCount = WaferMapCanvasDocument.ReticleBuilder.ReticleDieCount with { XCount = WaferDTO.WaferMapDataDTO.ReferenceDieRowNumber, YCount = WaferDTO.WaferMapDataDTO.ReferenceDieColumnNumber };
    }

    private void RefreshWaferMapDataSource()
    {
        WaferDTO.WaferMapDataDTO.WaferOriginalDiePoint = WaferMapCanvasDocument.DieBuilder.OriginalDiePoint;
        WaferDTO.WaferMapDataDTO.WaferReticleOriginalDiePoint = WaferMapCanvasDocument.ReticleBuilder.OriginalDiePoint;
        WaferDTO.WaferMapDataDTO.WaferDiameter = WaferMapCanvasViewModel.Document.WaferBuilder.Circle.Diameter;
        WaferDTO.WaferMapDataDTO.CellDieWidth = WaferMapCanvasViewModel.Document.DieBuilder.DiePitchSize.Width;
        WaferDTO.WaferMapDataDTO.CellDieHeight = WaferMapCanvasViewModel.Document.DieBuilder.DiePitchSize.Height;
        WaferDTO.WaferMapDataDTO.DieScribeWidth = WaferMapCanvasViewModel.Document.DieBuilder.DieScribeSize.Width;
        WaferDTO.WaferMapDataDTO.DieScribeHeight = WaferMapCanvasViewModel.Document.DieBuilder.DieScribeSize.Height;
        WaferDTO.WaferMapDataDTO.ReticleWidth = WaferMapCanvasViewModel.Document.ReticleBuilder.DiePitchSize.Width;
        WaferDTO.WaferMapDataDTO.ReticleHeight = WaferMapCanvasViewModel.Document.ReticleBuilder.DiePitchSize.Height;
        WaferDTO.WaferMapDataDTO.ReticleScribeWidth = WaferMapCanvasViewModel.Document.ReticleBuilder.DieScribeSize.Width;
        WaferDTO.WaferMapDataDTO.ReticleScribeHeight = WaferMapCanvasViewModel.Document.ReticleBuilder.DieScribeSize.Height;
        WaferDTO.WaferMapDataDTO.ReferenceDieRowNumber = WaferMapCanvasViewModel.Document.ReticleBuilder.ReticleDieCount.XCount;
        WaferDTO.WaferMapDataDTO.ReferenceDieColumnNumber = WaferMapCanvasViewModel.Document.ReticleBuilder.ReticleDieCount.YCount;
    }

    /// <summary>
    /// 通知所有 Wafer 参数属性已变更，刷新界面绑定
    /// </summary>
    private void NotifyWaferParametersChanged()
    {
        OnPropertyChanged(nameof(WaferRadius));
        OnPropertyChanged(nameof(WaferDiePitchSizeWidth));
        OnPropertyChanged(nameof(WaferDiePitchSizeHeight));
        OnPropertyChanged(nameof(WaferDieScribeSizeWidth));
        OnPropertyChanged(nameof(WaferDieScribeSizeHeight));
        OnPropertyChanged(nameof(WaferReticleDiePitchSizeWidth));
        OnPropertyChanged(nameof(WaferReticlePitchDieSizeHeight));
        OnPropertyChanged(nameof(WaferReticleDieScribeSizeWidth));
        OnPropertyChanged(nameof(WaferReticleDieScribeSizeHeight));
        OnPropertyChanged(nameof(WaferReticleDieCountX));
        OnPropertyChanged(nameof(WaferReticleDieCountY));
    }

    private void Lock()
    {
        using var scope = WaferMapCanvasDocument.View.Sync.EnterScope();
    }

    #endregion
}
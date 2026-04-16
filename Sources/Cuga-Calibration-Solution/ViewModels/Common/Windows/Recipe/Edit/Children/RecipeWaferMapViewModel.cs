using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Core.Models.Events;
using Core.Models.Models.Setting;
using Core.Recipe.Models;
using CugaCalibration.Core.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Graphics.Primitives.Enums.Editors;
using Net.Utilities.Graphics.Primitives.ObjectModels;
using Net.Utilities.IOC.Providers;
using Net.Utilities.Models.Geometries;
using Net.Utilities.WaferMap.WPF;
using Net.Utilities.WaferMap.WPF.Drawables;
using Net.Utilities.WaferMap.WPF.Editors;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;

namespace CugaCalibration.ViewModels.Common.Windows.Recipe.Edit.Children;

/// <summary>
/// 职责：WaferMap 绘制 / Die 选择 / 坐标计算
/// </summary>
[IOCAppService(ServiceType = typeof(RecipeWaferMapViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class RecipeWaferMapViewModel(
    ILogger<RecipeWaferMapViewModel> logger,
    IDialogWindowProvider dialogWindowProvider,
    ICalibrationRecipeService calibrationRecipeService,
    ISynchronizationContextProvider contextProvider,
    IMessenger messenger,
    StageViewModel stageViewModel,
    MicroscopeViewModel microscopeViewModel,
    CalibrationSetting calibrationSetting) : ViewModelBase, IRecipient<ValueChangedMessage<ToggleRecipeEvent>>
{
    #region 属性

    [ObservableProperty]
    private Point _stageDirection;

    /// <summary>
    /// 当前编辑中的配方（由主 VM LoadedAsync 赋值）
    /// </summary>
    public CalibrationRecipeDTO? EditingDTO { get; private set; }

    #region 界面

    [ObservableProperty]
    private bool _isAlignmentOk;

    [ObservableProperty]
    private WaferMapCanvasViewModel _waferMapCanvasViewModel = new();

    [ObservableProperty]
    private WaferMapDieSelectionInputOptions _waferMapDieSelectionInputOptions = new();

    public SelectionSet<WaferMapDie>? SelectionDies { get; private set; }

    //WaferMap 尺寸属性（转发到 Document）

    public double WaferRadius
    {
        get => WaferMapCanvasViewModel.Document.WaferBuilder.Circle.Radius;
        set
        {
            WaferMapCanvasViewModel.Document.WaferBuilder.Circle = new Circle(Point.Origin, value);
            RefreshWaferMapDataDTO();
            OnPropertyChanged();
        }
    }

    public double WaferDiePitchSizeWidth
    {
        get => WaferMapCanvasViewModel.Document.DieBuilder.DiePitchSize.Width;
        set
        {
            WaferMapCanvasViewModel.Document.DieBuilder.DiePitchSize = new Size(value, WaferMapCanvasViewModel.Document.DieBuilder.DiePitchSize.Height);
            RefreshWaferMapDataDTO();
            OnPropertyChanged();
        }
    }

    public double WaferDiePitchSizeHeight
    {
        get => WaferMapCanvasViewModel.Document.DieBuilder.DiePitchSize.Height;
        set
        {
            WaferMapCanvasViewModel.Document.DieBuilder.DiePitchSize = new Size(WaferMapCanvasViewModel.Document.DieBuilder.DiePitchSize.Width, value);
            RefreshWaferMapDataDTO();
            OnPropertyChanged();
        }
    }

    public double WaferDieScribeSizeWidth
    {
        get => WaferMapCanvasViewModel.Document.DieBuilder.DieScribeSize.Width;
        set
        {
            WaferMapCanvasViewModel.Document.DieBuilder.DieScribeSize = new Size(value, WaferMapCanvasViewModel.Document.DieBuilder.DieScribeSize.Height);
            RefreshWaferMapDataDTO();
            OnPropertyChanged();
        }
    }

    public double WaferDieScribeSizeHeight
    {
        get => WaferMapCanvasViewModel.Document.DieBuilder.DieScribeSize.Height;
        set
        {
            WaferMapCanvasViewModel.Document.DieBuilder.DieScribeSize = new Size(WaferMapCanvasViewModel.Document.DieBuilder.DieScribeSize.Width, value);
            RefreshWaferMapDataDTO();
            OnPropertyChanged();
        }
    }

    public double WaferReticleDiePitchSizeWidth
    {
        get => WaferMapCanvasViewModel.Document.ReticleBuilder.DiePitchSize.Width;
        set
        {
            WaferMapCanvasViewModel.Document.ReticleBuilder.DiePitchSize = new Size(value, WaferMapCanvasViewModel.Document.ReticleBuilder.DiePitchSize.Height);
            RefreshWaferMapDataDTO();
            OnPropertyChanged();
        }
    }

    public double WaferReticlePitchDieSizeHeight
    {
        get => WaferMapCanvasViewModel.Document.ReticleBuilder.DiePitchSize.Height;
        set
        {
            WaferMapCanvasViewModel.Document.ReticleBuilder.DiePitchSize = new Size(WaferMapCanvasViewModel.Document.ReticleBuilder.DiePitchSize.Width, value);
            RefreshWaferMapDataDTO();
            OnPropertyChanged();
        }
    }

    public double WaferReticleDieScribeSizeWidth
    {
        get => WaferMapCanvasViewModel.Document.ReticleBuilder.DieScribeSize.Width;
        set
        {
            WaferMapCanvasViewModel.Document.ReticleBuilder.DieScribeSize = new Size(value, WaferMapCanvasViewModel.Document.ReticleBuilder.DieScribeSize.Height);
            RefreshWaferMapDataDTO();
            OnPropertyChanged();
        }
    }

    public double WaferReticleDieScribeSizeHeight
    {
        get => WaferMapCanvasViewModel.Document.ReticleBuilder.DieScribeSize.Height;
        set
        {
            WaferMapCanvasViewModel.Document.ReticleBuilder.DieScribeSize = new Size(WaferMapCanvasViewModel.Document.ReticleBuilder.DieScribeSize.Width, value);
            RefreshWaferMapDataDTO();
            OnPropertyChanged();
        }
    }

    public int WaferReticleDieCountX
    {
        get => WaferMapCanvasViewModel.Document.ReticleBuilder.ReticleDieCount.XCount;
        set
        {
            WaferMapCanvasViewModel.Document.ReticleBuilder.ReticleDieCount = WaferMapCanvasViewModel.Document.ReticleBuilder.ReticleDieCount with { XCount = value };
            RefreshWaferMapDataDTO();
            OnPropertyChanged();
        }
    }

    public int WaferReticleDieCountY
    {
        get => WaferMapCanvasViewModel.Document.ReticleBuilder.ReticleDieCount.YCount;
        set
        {
            WaferMapCanvasViewModel.Document.ReticleBuilder.ReticleDieCount = WaferMapCanvasViewModel.Document.ReticleBuilder.ReticleDieCount with { YCount = value };
            RefreshWaferMapDataDTO();
            OnPropertyChanged();
        }
    }

    #endregion

    #endregion

    public void Initialize(CalibrationRecipeDTO dto, CancellationToken token)
    {
        EditingDTO = dto;

        messenger.UnregisterAll(this);
        messenger.RegisterAll(this);

        if (StageDirection == Point.Origin)
        {
            var (xDirection, yDirection) = stageViewModel.GetMachineDirection();
            StageDirection = new Point(xDirection, yDirection);
        }

        WaferMapCanvasViewModel.Document.Settings.IsShowToggleControlOfIsShowAxes = true;
        WaferMapCanvasViewModel.Document.Settings.IsShowToggleControlOfIsShowCursor = true;
        WaferMapCanvasViewModel.Document.Settings.IsShowToggleControlOfIsShowGrid = true;

        dto.WaferDTO.WaferMapDataToWaferMapCanvasDocument();
        WaferMapCanvasViewModel.Document = dto.WaferDTO.WaferMapCanvasDocument;

        NotifyWaferMapSetting();
        _ = Task.Factory.StartNew(async () =>
        {
            WaferMapCanvasViewModel.IsToggleSelection = false;
            WaferMapDieSelectionInputOptions.IsMultipleSelection = false;
            WaferMapDieSelectionInputOptions.CancellationToken = token;
            WaferMapDieSelectionInputOptions.Initialize();

            while (!token.IsCancellationRequested)
            {
                var inputResult = await WaferMapDieSelectionGetter
                    .RunAsync<WaferMapDieSelectionGetter>(WaferMapCanvasViewModel.Document.Editor, WaferMapDieSelectionInputOptions);

                if (inputResult.Output.Count > 1) continue;
                if (inputResult.InputResultModeEnum == InputResultModeEnum.Ok)
                {
                    foreach (var die in inputResult.Output)
                        die.IsSelected = true;
                }

                SelectionDies = inputResult.Output;
            }
        }, token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }

    #region 界面

    [RelayCommand]
    public async Task RefreshWaferMapAsync(object? name)
    {
        await Task.Run(() =>
        {
            try
            {
                // 重新BuildWafer需要重做P5和P8，更新WaferCenter和Alignment结果
                if (IsAlignmentOk == false)
                {
                    dialogWindowProvider.ShowDialog("The wafer map can only be refreshed after the alignment is successful. Please go to the Alignment page to action.", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    return;
                }

                if (name is null) return;
                var currentLens = microscopeViewModel.GetCurrentMicroscopeLensInformation();
                var configHighLens = calibrationSetting.SettingCommonParam.HighMicroscopeLensInformation;
                if (currentLens != configHighLens)
                {
                    dialogWindowProvider.ShowDialog($"The generation wafermap must to be done under a {configHighLens.LensName}. Please re-obtain the origin die coordinates ", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    microscopeViewModel.SwitchMicroscopeLensInformation(configHighLens);
                    return;
                }

                // Build Wafer
                Guard.IsNotNull(EditingDTO);
                var originDieWaferPosition = stageViewModel.GetBrightFieldStagePosition();
                EditingDTO.WaferDTO.WaferMapDataDTO.WaferOriginalDiePoint = originDieWaferPosition;
                EditingDTO.WaferDTO.WaferMapDataDTO.WaferReticleOriginalDiePoint = originDieWaferPosition;

                EditingDTO.WaferDTO.WaferMapDataToWaferMapCanvasDocument();
                contextProvider.Send(NotifyWaferMapView);
            }
            catch (Exception ex)
            {
                dialogWindowProvider.ShowDialog($"Refresh Wafer Map Failed! {ex.Message}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            }
        });
    }

    #endregion

    #region Command

    [RelayCommand]
    private async Task GotoPositionAsync()
    {
        await Task.Run(() =>
        {
            try
            {
                Guard.IsNotNull(EditingDTO);

                if (WaferMapCanvasViewModel.Document.ActiveView is null || SelectionDies is null) return;

                var waferMapDie = SelectionDies.ElementAt(0);

                calibrationRecipeService.GetWaferMapDieMachinePosition(EditingDTO.WaferDTO, waferMapDie, out var machinePosition);

                stageViewModel.SetMachineAbsoluteStageXy(machinePosition);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Goto Position failed");
            }
        }).ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task ReviewAsync()
    {
        await Task.Run(() =>
        {
            try
            {
                Guard.IsNotNull(EditingDTO);

                contextProvider.Send(() => calibrationRecipeService.GetCorrectWaferMapByOffset(EditingDTO.WaferDTO, true));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Update wafer map failed");
            }
        }).ConfigureAwait(false);
    }

    #endregion

    #region 状态刷新

    private void NotifyWaferMapSetting()
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

    private void RefreshWaferMapDataDTO()
    {
        if (EditingDTO is null) return;
        EditingDTO.WaferDTO.WaferMapDataDTO.WaferDiameter = WaferMapCanvasViewModel.Document.WaferBuilder.Circle.Diameter;
        EditingDTO.WaferDTO.WaferMapDataDTO.CellDieWidth = WaferMapCanvasViewModel.Document.DieBuilder.DiePitchSize.Width;
        EditingDTO.WaferDTO.WaferMapDataDTO.CellDieHeight = WaferMapCanvasViewModel.Document.DieBuilder.DiePitchSize.Height;
        EditingDTO.WaferDTO.WaferMapDataDTO.DieScribeWidth = WaferMapCanvasViewModel.Document.DieBuilder.DieScribeSize.Width;
        EditingDTO.WaferDTO.WaferMapDataDTO.DieScribeHeight = WaferMapCanvasViewModel.Document.DieBuilder.DieScribeSize.Height;
        EditingDTO.WaferDTO.WaferMapDataDTO.ReticleWidth = WaferMapCanvasViewModel.Document.ReticleBuilder.DiePitchSize.Width;
        EditingDTO.WaferDTO.WaferMapDataDTO.ReticleHeight = WaferMapCanvasViewModel.Document.ReticleBuilder.DiePitchSize.Height;
        EditingDTO.WaferDTO.WaferMapDataDTO.ReticleScribeWidth = WaferMapCanvasViewModel.Document.ReticleBuilder.DieScribeSize.Width;
        EditingDTO.WaferDTO.WaferMapDataDTO.ReticleScribeHeight = WaferMapCanvasViewModel.Document.ReticleBuilder.DieScribeSize.Height;
        EditingDTO.WaferDTO.WaferMapDataDTO.ReferenceDieRowNumber = WaferMapCanvasViewModel.Document.ReticleBuilder.ReticleDieCount.XCount;
        EditingDTO.WaferDTO.WaferMapDataDTO.ReferenceDieColumnNumber = WaferMapCanvasViewModel.Document.ReticleBuilder.ReticleDieCount.YCount;
    }

    public void NotifyWaferMapView()
    {
        WaferMapCanvasViewModel.Document.OverlayCrossLine.Point = WaferMapCanvasViewModel.Document.DieBuilder.OriginalDiePoint;
        WaferMapCanvasViewModel.Document.OverlayCrossLine.IsVisible = true;
        OnPropertyChanged(nameof(WaferMapCanvasViewModel));
    }

    public void Receive(ValueChangedMessage<ToggleRecipeEvent> message)
    {
        contextProvider.Post(() =>
        {
            if (message.Value.IsRecipeAlignment.HasValue)
                IsAlignmentOk = message.Value.IsRecipeAlignment.Value;
        });
    }

    #endregion
}
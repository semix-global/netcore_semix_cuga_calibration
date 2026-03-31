using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Models.Chuck.CenterAndTheta;
using Core.Models.Models.Setting;
using Core.Recipe.Models;
using CugaCalibration.Core.Services.Interfaces;
using Local.SQL.Cache.Providers.Extensions;
using Local.SQL.Cache.Providers.Interfaces;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Graphics.Primitives.Enums.Editors;
using Net.Utilities.Graphics.Primitives.ObjectModels;
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
    ICacheProvider cacheProvider,
    ILogger<RecipeWaferMapViewModel> logger,
    IDialogWindowProvider dialogWindowProvider,
    ICalibrationRecipeService calibrationRecipeService,
    StageViewModel stageViewModel,
    MicroscopeViewModel microscopeViewModel,
    CalibrationSetting calibrationSetting,
    RecipeCookie recipeCookie) : ViewModelBase
{
    // ── 对外暴露 ─────────────────────────────────────────────────────────────

    [ObservableProperty]
    private WaferMapCanvasViewModel _waferMapCanvasViewModel = new();

    [ObservableProperty]
    private WaferMapDieSelectionInputOptions _waferMapDieSelectionInputOptions = new();

    public SelectionSet<WaferMapDie>? SelectionDies { get; private set; }

    /// <summary>当前编辑中的配方（由主 VM LoadedAsync 赋值）</summary>
    public CalibrationRecipeDTO? EditingDto { get; set; }

    // ── 属性注入（由主 VM LoadedAsync 赋值） ──────────────────────────────────

    /// <summary>
    /// 由主 VM 赋值。调用后取消旧 Token 并返回新 Token，
    /// 用于重启 SelectionDiesAsync 循环。
    /// </summary>
    public Func<CancellationToken>? RefreshTokenFunc { get; set; }

    // ── 内部状态 ─────────────────────────────────────────────────────────────

    private Point _stageDirection = Point.Origin;

    // ── WaferMap 尺寸属性（转发到 Document） ─────────────────────────────────

    public double WaferRadius
    {
        get => WaferMapCanvasViewModel.Document.WaferBuilder.Circle.Radius;
        set
        {
            WaferMapCanvasViewModel.Document.WaferBuilder.Circle = new Circle(Point.Origin, value);
            OnPropertyChanged();
        }
    }

    public double WaferDiePitchSizeWidth
    {
        get => WaferMapCanvasViewModel.Document.DieBuilder.DiePitchSize.Width;
        set
        {
            WaferMapCanvasViewModel.Document.DieBuilder.DiePitchSize = new Size(value, WaferMapCanvasViewModel.Document.DieBuilder.DiePitchSize.Height);
            OnPropertyChanged();
        }
    }

    public double WaferDiePitchSizeHeight
    {
        get => WaferMapCanvasViewModel.Document.DieBuilder.DiePitchSize.Height;
        set
        {
            WaferMapCanvasViewModel.Document.DieBuilder.DiePitchSize = new Size(WaferMapCanvasViewModel.Document.DieBuilder.DiePitchSize.Width, value);
            OnPropertyChanged();
        }
    }

    public double WaferDieScribeSizeWidth
    {
        get => WaferMapCanvasViewModel.Document.DieBuilder.DieScribeSize.Width;
        set
        {
            WaferMapCanvasViewModel.Document.DieBuilder.DieScribeSize = new Size(value, WaferMapCanvasViewModel.Document.DieBuilder.DieScribeSize.Height);
            OnPropertyChanged();
        }
    }

    public double WaferDieScribeSizeHeight
    {
        get => WaferMapCanvasViewModel.Document.DieBuilder.DieScribeSize.Height;
        set
        {
            WaferMapCanvasViewModel.Document.DieBuilder.DieScribeSize = new Size(WaferMapCanvasViewModel.Document.DieBuilder.DieScribeSize.Width, value);
            OnPropertyChanged();
        }
    }

    public double WaferReticleDiePitchSizeWidth
    {
        get => WaferMapCanvasViewModel.Document.ReticleBuilder.DiePitchSize.Width;
        set
        {
            WaferMapCanvasViewModel.Document.ReticleBuilder.DiePitchSize = new Size(value, WaferMapCanvasViewModel.Document.ReticleBuilder.DiePitchSize.Height);
            OnPropertyChanged();
        }
    }

    public double WaferReticlePitchDieSizeHeight
    {
        get => WaferMapCanvasViewModel.Document.ReticleBuilder.DiePitchSize.Height;
        set
        {
            WaferMapCanvasViewModel.Document.ReticleBuilder.DiePitchSize = new Size(WaferMapCanvasViewModel.Document.ReticleBuilder.DiePitchSize.Width, value);
            OnPropertyChanged();
        }
    }

    public double WaferReticleDieScribeSizeWidth
    {
        get => WaferMapCanvasViewModel.Document.ReticleBuilder.DieScribeSize.Width;
        set
        {
            WaferMapCanvasViewModel.Document.ReticleBuilder.DieScribeSize = new Size(value, WaferMapCanvasViewModel.Document.ReticleBuilder.DieScribeSize.Height);
            OnPropertyChanged();
        }
    }

    public double WaferReticleDieScribeSizeHeight
    {
        get => WaferMapCanvasViewModel.Document.ReticleBuilder.DieScribeSize.Height;
        set
        {
            WaferMapCanvasViewModel.Document.ReticleBuilder.DieScribeSize = new Size(WaferMapCanvasViewModel.Document.ReticleBuilder.DieScribeSize.Width, value);
            OnPropertyChanged();
        }
    }

    public int WaferReticleDieCountX
    {
        get => WaferMapCanvasViewModel.Document.ReticleBuilder.ReticleDieCount.XCount;
        set
        {
            WaferMapCanvasViewModel.Document.ReticleBuilder.ReticleDieCount = WaferMapCanvasViewModel.Document.ReticleBuilder.ReticleDieCount with { XCount = value };
            OnPropertyChanged();
        }
    }

    public int WaferReticleDieCountY
    {
        get => WaferMapCanvasViewModel.Document.ReticleBuilder.ReticleDieCount.YCount;
        set
        {
            WaferMapCanvasViewModel.Document.ReticleBuilder.ReticleDieCount = WaferMapCanvasViewModel.Document.ReticleBuilder.ReticleDieCount with { YCount = value };
            OnPropertyChanged();
        }
    }

    // ── 初始化（由主 VM LoadedAsync 调用） ───────────────────────────────────

    public void Initialize(CalibrationRecipeDTO dto, CancellationToken token)
    {
        EditingDto = dto;

        if (_stageDirection == Point.Origin)
        {
            var (xDirection, yDirection) = stageViewModel.GetMachineDirection();
            _stageDirection = new Point(xDirection, yDirection);
        }

        WaferMapCanvasViewModel.Document.Settings.IsShowToggleControlOfIsShowAxes = true;
        WaferMapCanvasViewModel.Document.Settings.IsShowToggleControlOfIsShowCursor = true;
        WaferMapCanvasViewModel.Document.Settings.IsShowToggleControlOfIsShowGrid = true;

        dto.WaferDto.WaferMapDataToWaferMapCanvasDocument();
        WaferMapCanvasViewModel.Document = dto.WaferDto.WaferMapCanvasDocument;

        NotifyWaferMapSetting();
        _ = Task.Factory.StartNew(() => SelectionDiesAsync(token), token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }

    // ── Die 选择循环 ─────────────────────────────────────────────────────────

    public async Task SelectionDiesAsync(CancellationToken token)
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
    }

    // ── 刷新 WaferMap ─────────────────────────────────────────────────────────

    [RelayCommand]
    public async Task RefreshWaferMapAsync(object? name)
    {
        await Task.Run(() =>
        {
            if (name is null) return;
            var isReticle = name.ToString() == "Reticle";
            var currentLens = microscopeViewModel.GetCurrentMicroscopeLensInformation();
            var configHighLens = calibrationSetting.SettingCommonParam.HighMicroscopeLensInformation;
            if (currentLens != configHighLens)
            {
                dialogWindowProvider.ShowDialog($"The generation wafermap must to be done under a {configHighLens.LensName}. Please re-obtain the origin die coordinates ", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                microscopeViewModel.SwitchMicroscopeLensInformation(configHighLens);
                return;
            }

            if (GenerateWaferMap(recipeCookie.CalibrationRecipeDto) == false)
                dialogWindowProvider.ShowDialog("Generate Wafer Map Failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
        });
    }

    public bool GenerateWaferMap(CalibrationRecipeDTO dto)
    {
        try
        {
            if (dto.WaferDto.WaferCenterWaferPosition is null)
            {
                dialogWindowProvider.ShowDialog("The wafer center position is not set!",
                    DialogButtonsEnum.OK,
                    DialogIconEnum.Warning);
                return false;
            }

            var chuckCenter = cacheProvider.GetOrDefault<ChuckCenterAndThetaItemDto>();
            var originDieMachinePosition = stageViewModel.GetMachineStagePosition();
            var originDieMachineOffset = originDieMachinePosition - chuckCenter.NewBFCenterStagePosition;
            var originDieWaferPosition =
                new Point(_stageDirection.X * originDieMachineOffset.X, _stageDirection.Y * originDieMachineOffset.Y)
                - (Vector)dto.WaferDto.WaferCenterWaferPosition!.Value;

            WaferMapCanvasViewModel.Document.DieBuilder.OriginalDiePoint = originDieWaferPosition;
            WaferMapCanvasViewModel.Document.ReticleBuilder.OriginalDiePoint = originDieWaferPosition;

            NotifyWaferMapView();
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Generate Wafer Map Failed!");
            return false;
        }
    }

    public void SetDocumentFromDto(CalibrationRecipeDTO dto)
    {
        dto.WaferDto.WaferMapDataToWaferMapCanvasDocument();
        WaferMapCanvasViewModel.Document = dto.WaferDto.WaferMapCanvasDocument;
        NotifyWaferMapView();
    }

    #region Command

    [RelayCommand]
    private async Task GotoPositionAsync()
    {
        await Task.Run(() =>
        {
            try
            {
                Guard.IsNotNull(EditingDto);

                dialogWindowProvider.TryShowDialog("Do you want to use review mode ?", out var dialogResult, DialogButtonsEnum.YesNo, DialogIconEnum.Question);
                var isReview = dialogResult == DialogResultEnum.Yes;

                var document = WaferMapCanvasViewModel.Document;
                if (document.ActiveView is null || SelectionDies is null) return;

                var index = SelectionDies.ElementAt(0).Index;
                var centerPosition = isReview
                    ? recipeCookie.CalibrationReviseRecipeDto.WaferDto.WaferCenterWaferPosition
                    : EditingDto.WaferDto.WaferCenterWaferPosition!;

                var waferPosition = new Point(
                    document.OriginalDie.Rect.X + index.X * document.DieBuilder.DieSize.Width,
                    document.OriginalDie.Rect.Y + index.Y * document.DieBuilder.DieSize.Height);

                var position = centerPosition!.Value + (Vector)waferPosition;
                stageViewModel.SetBrightFieldAbsoluteStageXy(position);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Goto Position failed");
            }
        }).ConfigureAwait(false);
    }

    [RelayCommand]
    public async Task ReviewAsync()
    {
        await Task.Run(() =>
        {
            try
            {
                Guard.IsNotNull(EditingDto);

                var token = RefreshTokenFunc?.Invoke() ?? CancellationToken.None;

                var reviseRecipeDto = calibrationRecipeService.GetCorrectWaferMapByOffset(EditingDto, true);
                SetDocumentFromDto(reviseRecipeDto);
                // todo
                _ = Task.Factory.StartNew(
                    () => SelectionDiesAsync(token),
                    token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Update wafer map failed");
            }
        }).ConfigureAwait(false);
    }

    #endregion

    // ── 通知辅助 ──────────────────────────────────────────────────────────────

    public void NotifyWaferMapView()
    {
        WaferMapCanvasViewModel.Document.OverlayCrossLine.Point = WaferMapCanvasViewModel.Document.DieBuilder.OriginalDiePoint;
        WaferMapCanvasViewModel.Document.OverlayCrossLine.IsVisible = true;
        OnPropertyChanged(nameof(WaferMapCanvasViewModel));
    }

    public void NotifyWaferMapSetting()
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

    public Point StageDirection => _stageDirection;
}
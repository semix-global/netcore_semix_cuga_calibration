using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Core.Models.Enums.Algorithm;
using Core.Models.Enums.Stage;
using Core.Models.Events;
using Core.Models.Helper;
using Core.Models.Models.Chuck.Center;
using Core.Models.Models.Common.Alignment;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Common.Recipe;
using Core.Models.Models.Common.Recipe.Wafer.ReticleMask;
using Core.Models.Models.Microscope.PixelSize;
using Core.Utilities;
using CugaCalibration.Core.Services.Interfaces;
using CugaCalibration.ViewModels.Common.Windows.Tools;
using CugaCalibration.ViewModels.Common.Windows.Tools.Alignment;
using Local.NoSQL.DB.Providers.Helper;
using Local.NoSQL.DB.Providers.Interfaces;
using Local.SQL.DB.Providers.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MoreLinq;
using Net.Utilities.Algorithms.Halcon;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Graphics.Primitives.Enums.Editors;
using Net.Utilities.Graphics.Primitives.ObjectModels;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.IOC.Providers;
using Net.Utilities.Models;
using Net.Utilities.Models.Geometries;
using Net.Utilities.WaferMap.WPF;
using Net.Utilities.WaferMap.WPF.Drawables;
using Net.Utilities.WaferMap.WPF.Editors;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.Services;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Windows.Controls;
using System.Windows.Input;

namespace CugaCalibration.ViewModels.Common.Windows.Management.Recipe;

[IOCAppService(ServiceType = typeof(RecipeSettingViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class RecipeSettingViewModel(
    ICacheProvider cacheProvider,
    [FromKeyedServices(LiteDbConstantHelper.RecipeDbKey)]
    ICacheProvider recipeCacheProvider,
    ILogger<RecipeSettingViewModel> logger,
    IDialogWindowProvider dialogWindowProvider,
    IWindowManagerService windowManagerService,
    IMessenger messenger,
    ISynchronizationContextProvider synchronizationContext,
    ICalibrationRecipeService calibrationRecipeService,
    IOptions<ApplicationSetting> options,
    ISysRecipeInformationService sysRecipeInformationService,
    [FromKeyedServices(LiteDbConstantHelper.RecipeDbKey)]
    ILiteDatabaseProvider liteDatabaseProvider,
    CreateDarkImageTemplateWindowViewModel createDarkImageTemplateWindowViewModel,
    AlignmentWindowDarkFieldViewModel alignmentWindowDarkFieldViewModel,
    StageViewModel stageViewModel,
    MicroscopeViewModel microscopeViewModel,
    ReviewViewModel reviewViewModel,
    LaserViewModel laserViewModel,
    ApplicationCookie applicationCookie) : ViewModelBase
{
    private readonly string _appHomeDirectory = options.Value.AppHomeDirectory;
    public readonly StageViewModel StageViewModel = stageViewModel;

    public AlignmentWindowDarkFieldViewModel AlignmentWindowDarkFieldViewModel { get; } = alignmentWindowDarkFieldViewModel;
    private CancellationTokenSource? _cancellationTokenSource;

    #region 字段

    /// <summary>
    /// 模板存储位置
    /// </summary>
    private string TemplateFileDirectory => Path.Combine(_appHomeDirectory, "Template", "Recipe", EditRecipeTypeName, DateTime.Now.ToString(Constants.ShortFileDateTimeFormat));

    public SelectionSet<WaferMapDie>? _selectionDies;

    private Point _stageDirection = Point.Origin;

    #endregion 字段

    #region 界面显示

    public ApplicationCookie ApplicationCookie => applicationCookie;

    [ObservableProperty]
    private bool _isReticleMode;

    [ObservableProperty]
    private bool _isEditWaferMapEnable = true;

    [ObservableProperty]
    private CalibrationRecipeDto _calibrationRecipeDto = new();

    [ObservableProperty]
    private CalibrationRecipeDto? _selectRecipeDtoBackup;

    [ObservableProperty]
    private ReticleMarkItemDto? _selectReticleMarkItem;

    /// <summary>
    /// 选中TabItem的content
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<ReticleMarkItemDto> _editReticleMarkList = [];

    /// <summary>
    /// 用于reticleMaskView绘制用的binding对象
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<ReticleMarkItemDto> _reticleMarkList = [];

    [ObservableProperty]
    private WaferMapCanvasViewModel _waferMapCanvasViewModel = new();

    /// <summary>
    ///  当前编辑的配方参数对象
    /// </summary>
    [ObservableProperty]
    private object? _editRecipeTypeObj;

    private string EditRecipeTypeName
    {
        get
        {
            if (EditRecipeTypeObj is null || EditRecipeTypeObj is not TabItem tabItem)
                return string.Empty;
            var value = tabItem.Header.ToString();
            return value;
        }
    }

    #endregion 界面显示

    #region Review字段属性

    private bool _isLoaded = false;

    [ObservableProperty]
    private bool _isReview;

    [ObservableProperty]
    private CalibrationRecipeDto? _reviewRecipeDtoBackup;

    #endregion

    #region WaferMap属性

    [ObservableProperty]
    private WaferMapDieSelectionInputOptions _waferMapDieSelectionInputOptions = new();

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

    #endregion

    #region 业务

    [RelayCommand]
    private async Task LoadedAsync()
    {
        if (_isLoaded) return;
        _isLoaded = true;
        if (_stageDirection == Point.Origin)
        {
            var (xDirection, yDirection) = StageViewModel.GetMachineDirection();
            _stageDirection = new Point(xDirection, yDirection);
        }

        SelectRecipeDtoBackup = CalibrationRecipeDto.Clone();

        if (CalibrationRecipeDto.CalibrationRecipeInfoDto.RecipeNosqlRecipeDbDataSource == string.Empty) // 新增的配方
        {
            var initialDbDirectoryPath = Path.Combine(options.Value.NosqlDbDataSourceDirectory, CalibrationRecipeDto.CalibrationRecipeInfoDto.RecipeName);
            var newPath = FolderHelper.GenerateIndexedDirectoryPath(initialDbDirectoryPath, options.Value.NosqlDbDataSourceDirectory);
            CalibrationRecipeDto.CalibrationRecipeInfoDto.RecipeNosqlRecipeDbDataSource = Path.Combine(newPath, Path.GetFileName(options.Value.NosqlDbDataSource));
            CalibrationRecipeDto.CalibrationRecipeInfoDto.RecipeName = new DirectoryInfo(newPath).Name;
            SelectRecipeDtoBackup = CalibrationRecipeDto.Clone();
            await SaveAsync().ConfigureAwait(false);
        }

        // todo:判断校准状态都为ok时才可以编辑
        IsEditWaferMapEnable = CalibrationRecipeDto.WaferDto.RequireActionIsOk();
        WaferMapCanvasViewModel.Document.Settings.IsCanToggleAxes = true;
        WaferMapCanvasViewModel.Document.Settings.IsCanToggleCursor = true;
        WaferMapCanvasViewModel.Document.Settings.IsCanToggleGrid = true;

        CalibrationRecipeDto.WaferDto.WaferMapDataToWaferMapCanvasDocument();
        WaferMapCanvasViewModel.Document = CalibrationRecipeDto.WaferDto.WaferMapCanvasDocument;
        RefreshToken();
        _ = Task.Factory.StartNew(() => SelectionDiesAsync(_cancellationTokenSource.Token), _cancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        NotifyWaferMapSetting();

        if (ApplicationCookie.CalibrationRecipeDto == null)
        {
            dialogWindowProvider.ShowDialog("Recipe cache is empty!");
            return;
        }

        ReviewRecipeDtoBackup = ApplicationCookie.CalibrationRecipeDto.Clone();

        if (CalibrationRecipeDto.CalibrationRecipeInfoDto.MicroscopeLowMag.LensCode == -1)
            CalibrationRecipeDto.CalibrationRecipeInfoDto.MicroscopeLowMag = ApplicationCookie.MicroscopeLensInformationList[0];

        if (CalibrationRecipeDto.CalibrationRecipeInfoDto.MicroscopeHighMag.LensCode == -1)
            CalibrationRecipeDto.CalibrationRecipeInfoDto.MicroscopeHighMag = ApplicationCookie.MicroscopeLensInformationList.Count <= 2
                ? ApplicationCookie.MicroscopeLensInformationList[^1]
                : ApplicationCookie.MicroscopeLensInformationList[2];
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            CalibrationRecipeDto.WaferDto.WaferMapCanvasDocumentToWaferMapData();
            // 配方命名不能为空
            var recipeInfo = CalibrationRecipeDto.CalibrationRecipeInfoDto;
            if (recipeInfo.RecipeName == string.Empty)
            {
                dialogWindowProvider.ShowDialog("Recipe name is empty!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return;
            }

            recipeInfo.RecipeNosqlRecipeDbDataSource = Path.Combine(options.Value.NosqlDbDataSourceDirectory, recipeInfo.RecipeName, recipeInfo.RecipeDbName);

            // 写入sqlLite数据库
            var recipeInfoEntityDto = recipeInfo.AdaptTo();
            if (await sysRecipeInformationService.InsertAsync(recipeInfoEntityDto).ConfigureAwait(false) == false)
            {
                dialogWindowProvider.ShowDialog("Save Recipe Information failed! Please check if the name already exists!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return;
            }

            try
            {
                // 重命名的情况
                if (SelectRecipeDtoBackup!.CalibrationRecipeInfoDto.RecipeName != recipeInfo.RecipeName
                    && SelectRecipeDtoBackup!.CalibrationRecipeInfoDto.RecipeNosqlRecipeDbDataSource != string.Empty)
                {
                    liteDatabaseProvider.Dispose();
                    var backupDbDirectoryPath = Path.GetDirectoryName(SelectRecipeDtoBackup!.CalibrationRecipeInfoDto.RecipeNosqlRecipeDbDataSource);
                    Directory.Move(backupDbDirectoryPath, Path.GetDirectoryName(recipeInfo.RecipeNosqlRecipeDbDataSource));
                }

                // 写入nosql数据库
                if (liteDatabaseProvider.ModifyLiteDatabase(recipeInfo.RecipeNosqlRecipeDbDataSource) == false)
                {
                    dialogWindowProvider.ShowDialog("Get lite database failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    return;
                }

                recipeCacheProvider.Set(CalibrationRecipeDto, CancellationToken.None);
            }
            catch (Exception ex)
            {
                await sysRecipeInformationService.DeleteAsync(recipeInfoEntityDto).ConfigureAwait(false);
                logger.LogError(ex, "{@Name} Save recipe to liteDb failed!", nameof(RecipeSettingViewModel));
                return;
            }

            dialogWindowProvider.TryShowDialog("Do you want to apply this recipe? ", out DialogResultEnum dialogResultEnum, DialogButtonsEnum.YesNo, DialogIconEnum.Question);
            if (dialogResultEnum == DialogResultEnum.Yes)
            {
                applicationCookie.CalibrationRecipeDto = CalibrationRecipeDto.Clone();
                applicationCookie.CalibrationReviseRecipeDto = CalibrationRecipeDto.Clone();
                messenger.Send(ToggleCalibrateEventFactory.RefreshMenuStatus(true)); // 刷新界面
                messenger.Send(ToggleRecipeEventFactory.RefreshRecipeManagementView(true));
            }

            Close();
        }
        catch (Exception ex)
        {
            dialogWindowProvider.ShowDialog($"Save Recipe failed! {ex.Message}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
        }
    }

    [RelayCommand]
    private void Close()
    {
        CloseView(null);
        messenger.Send(ToggleRecipeEventFactory.RefreshRecipeManagementView(true));
        CancelToken();
        _isLoaded = false;
        applicationCookie.CalibrationRecipeDto = ReviewRecipeDtoBackup.CalibrationRecipeInfoDto.RecipeName == CalibrationRecipeDto.CalibrationRecipeInfoDto.RecipeName
            ? CalibrationRecipeDto.Clone()
            : ReviewRecipeDtoBackup!.Clone();
    }

    private void CancelToken()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
    }

    [MemberNotNull(nameof(_cancellationTokenSource))]
    private void RefreshToken()
    {
        CancelToken();

        _cancellationTokenSource = new CancellationTokenSource();
    }

    #endregion 业务

    #region wafer Map

    private async Task SelectionDiesAsync(CancellationToken token)
    {
        WaferMapCanvasViewModel.IsToggleSelection = false;

        WaferMapDieSelectionInputOptions.IsMultipleSelection = false;
        WaferMapDieSelectionInputOptions.CancellationToken = token;
        WaferMapDieSelectionInputOptions.Initialize();

        while (token.IsCancellationRequested == false)
        {
            var inputResult = await WaferMapDieSelectionGetter
                .RunAsync<WaferMapDieSelectionGetter>(WaferMapCanvasViewModel.Document.Editor, WaferMapDieSelectionInputOptions);
            if (inputResult.Output.Count > 1) continue;
            if (inputResult.InputResultModeEnum == InputResultModeEnum.Ok)
            {
                foreach (var waferMapDie in inputResult.Output)
                {
                    waferMapDie.IsSelected = true;
                }
            }

            _selectionDies = inputResult.Output;
        }

        // WaferMapCanvasViewModel.IsToggleSelection = true;
    }

    [RelayCommand]
    private async Task RefreshWaferMapAsync(object? name)
    {
        await Task.Run(() =>
        {
            if (name is null) return;
            IsReticleMode = name.ToString() == "Reticle";
            var currentMicroscopeLensInformation = microscopeViewModel.GetCurrentMicroscopeLensInformation();
            if (currentMicroscopeLensInformation != CalibrationRecipeDto.CalibrationRecipeInfoDto.MicroscopeHighMag)
            {
                dialogWindowProvider.ShowDialog("The generation wafermap must to be done under a 50x lens. Please re-obtain the origin die coordinates ", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                microscopeViewModel.SwitchMicroscopeLensInformation(CalibrationRecipeDto.CalibrationRecipeInfoDto.MicroscopeHighMag);
            }

            if (GenerateWaferMap() == false)
                dialogWindowProvider.ShowDialog("Generate Wafer Map Failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
        });
    }

    private bool GenerateWaferMap()
    {
        try
        {
            if (CalibrationRecipeDto.WaferDto.WaferCenterWaferPosition is null)
            {
                dialogWindowProvider.ShowDialog("The wafer center position is not set!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }

            var chuckCenterMachinePosition = cacheProvider.GetOrDefault<ChuckCenterObjDto>();
            var originDieMachinePosition = StageViewModel.GetMachineStagePosition();
            var originDieMachineOffset = originDieMachinePosition - chuckCenterMachinePosition.NewBFCenterStagePosition;
            var originDieWaferPosition = new Point(_stageDirection.X * originDieMachineOffset.X, _stageDirection.Y * originDieMachineOffset.Y)
                                         - (Vector)CalibrationRecipeDto.WaferDto.WaferCenterWaferPosition!.Value;

            WaferMapCanvasViewModel.Document.DieBuilder.OriginalDiePoint = originDieWaferPosition;
            WaferMapCanvasViewModel.Document.ReticleBuilder.OriginalDiePoint = originDieWaferPosition;

            NotifyWaferMapView();
            IsReview = false;
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Generate Wafer Map Failed!");
            return false;
        }
    }

    private void NotifyWaferMapView()
    {
        WaferMapCanvasViewModel.Document.OverlayCrossLine.Point = WaferMapCanvasViewModel.Document.DieBuilder.OriginalDiePoint;
        WaferMapCanvasViewModel.Document.OverlayCrossLine.IsVisible = true;
        OnPropertyChanged(nameof(WaferMapCanvasViewModel));
    }

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

    #endregion wafer Map

    #region Review

    [RelayCommand]
    private async Task ReviewAsync()
    {
        await Task.Run(() =>
        {
            try
            {
                RefreshToken();
                applicationCookie.CalibrationRecipeDto = CalibrationRecipeDto.Clone();
                if (calibrationRecipeService.GetCorrectWaferMapByOffset(true) == false)
                    dialogWindowProvider.ShowDialog("Get correct wafer map by offset failed!");
                RefreshReviewWaferMapCanvas(applicationCookie.CalibrationReviseRecipeDto);

                _ = Task.Factory.StartNew(() => SelectionDiesAsync(_cancellationTokenSource.Token), _cancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);

                IsReview = true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Update wafer map failed");
            }
        }).ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task GotoPositionAsync()
    {
        await Task.Run(() =>
        {
            try
            {
                var document = WaferMapCanvasViewModel.Document;
                if (document.ActiveView is null) return;

                if (_selectionDies is null) return;

                var index = _selectionDies.ElementAt(0).Index;
                var centerPosition = IsReview
                    ? ApplicationCookie.CalibrationReviseRecipeDto!.WaferDto.WaferCenterWaferPosition
                    : CalibrationRecipeDto.WaferDto.WaferCenterWaferPosition!;
                var waferPosition = new Point(document.OriginalDie.Rect.X + index.X * document.DieBuilder.DieSize.Width,
                    document.OriginalDie.Rect.Y + index.Y * document.DieBuilder.DieSize.Height);
                var position = centerPosition!.Value + (Vector)waferPosition;

                StageViewModel.SetBrightFieldAbsoluteStageXy(position);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Goto Position failed");
            }
        }).ConfigureAwait(false);
    }

    private bool RefreshReviewWaferMapCanvas(CalibrationRecipeDto? calibrationRecipeDto)
    {
        try
        {
            if (calibrationRecipeDto is null)
            {
                dialogWindowProvider.ShowDialog("The wafer map is not set!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }

            calibrationRecipeDto.WaferDto.WaferMapDataToWaferMapCanvasDocument();
            WaferMapCanvasViewModel.Document = calibrationRecipeDto.WaferDto.WaferMapCanvasDocument;

            NotifyWaferMapView();
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Generate Wafer Map Failed!");
            return false;
        }
    }

    #endregion

    #region MaskConfig

    [RelayCommand]
    private void AddMask(object? name)
    {
        // todo:新增时判断有无重复项
        if (name is null) return;
        synchronizationContext.Send(() =>
        {
            var reticleMarkList = new ObservableCollection<ReticleMarkItemDto>();
            switch (name.ToString())
            {
                case nameof(CalibrationRecipeDto.WaferDto.ReticleMarkDto.MicrosocpeReticleMarkItemList):
                    reticleMarkList = CalibrationRecipeDto.WaferDto.ReticleMarkDto.MicrosocpeReticleMarkItemList;
                    break;

                case nameof(CalibrationRecipeDto.WaferDto.ReticleMarkDto.ChuckReticleMarkItemList):
                    reticleMarkList = CalibrationRecipeDto.WaferDto.ReticleMarkDto.ChuckReticleMarkItemList;
                    break;

                case nameof(CalibrationRecipeDto.WaferDto.ReticleMarkDto.LaserReticleMarkItemList):
                    reticleMarkList = CalibrationRecipeDto.WaferDto.ReticleMarkDto.LaserReticleMarkItemList;
                    break;
            }

            reticleMarkList.Add(new ReticleMarkItemDto()
            {
                MaskIndex = reticleMarkList.Count != 0 ? reticleMarkList.Last().MaskIndex + 1 : 0
            });
        });
    }

    [RelayCommand]
    private void DeleteMask(string name)
    {
        if (SelectReticleMarkItem is null)
            return;
        synchronizationContext.Send(() =>
        {
            var reticleMarkList = new ObservableCollection<ReticleMarkItemDto>();
            switch (name)
            {
                case nameof(CalibrationRecipeDto.WaferDto.ReticleMarkDto.MicrosocpeReticleMarkItemList):
                    reticleMarkList = CalibrationRecipeDto.WaferDto.ReticleMarkDto.MicrosocpeReticleMarkItemList;
                    break;

                case nameof(CalibrationRecipeDto.WaferDto.ReticleMarkDto.ChuckReticleMarkItemList):
                    reticleMarkList = CalibrationRecipeDto.WaferDto.ReticleMarkDto.ChuckReticleMarkItemList;
                    break;

                case nameof(CalibrationRecipeDto.WaferDto.ReticleMarkDto.LaserReticleMarkItemList):
                    reticleMarkList = CalibrationRecipeDto.WaferDto.ReticleMarkDto.LaserReticleMarkItemList;
                    break;
            }

            for (var i = SelectReticleMarkItem.MaskIndex + 1; i < reticleMarkList.Count; i++)
            {
                reticleMarkList[i].MaskIndex -= 1;
            }

            reticleMarkList.Remove(SelectReticleMarkItem);
        });
    }

    #endregion MaskConfig

    #region Reticle Mask

    #region Template Command

    // todo:增加die模式
    [RelayCommand]
    private void GetMaskReticlePosition(object? obj)
    {
        try
        {
            if (obj is null)
                return;
            var (maskDto, _) = GetSelectReticleMaskListInfo(obj.ToString());
            var waferBuilder = WaferMapCanvasViewModel.Document.WaferBuilder;
            var reticleBuilder = WaferMapCanvasViewModel.Document.ReticleBuilder;
            var machinePosition = StageViewModel.GetMachineStagePosition();
            if (machinePosition.ToOriginLength >= waferBuilder.Circle.Radius)
            {
                dialogWindowProvider.ShowDialog("The position out of the wafer range!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return;
            }

            var chuckCenterMachinePosition = cacheProvider.GetOrDefault<ChuckCenterObjDto>();
            var waferPosition = machinePosition - (Vector)chuckCenterMachinePosition.NewBFCenterStagePosition;

            var diePitchHeight = reticleBuilder.DiePitchSize.Height;
            var scribeSize = reticleBuilder.DieScribeSize;

            var relativeReticleOriginPosition = new Point(_stageDirection.X * waferPosition.X, _stageDirection.Y * waferPosition.Y)
                                                - (Vector)(reticleBuilder.OriginalDiePoint - (Vector)new Point(0, diePitchHeight + scribeSize.Height));

            maskDto.MaskWaferCellPosition = relativeReticleOriginPosition;
            RefreshReticleMaskView();
        }
        catch (Exception ex)
        {
            dialogWindowProvider.ShowDialog("Get Mask Reticle Position Failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            logger.LogError(ex, "Get Mask Reticle Position Failed!");
        }
    }

    // todo:增加die模式
    [RelayCommand]
    private void GotoMaskReticlePosition(object? obj)
    {
        try
        {
            if (obj is null)
                return;
            var (maskDto, _) = GetSelectReticleMaskListInfo(obj.ToString());
            microscopeViewModel.SwitchMicroscopeLensInformation(maskDto.RecipeBrightFieldTemplateDto.MicroscopeLensInformation);

            var reticleBuilder = WaferMapCanvasViewModel.Document.ReticleBuilder;

            var diePitchHeight = reticleBuilder.DiePitchSize.Height;
            var scribeSize = reticleBuilder.DieScribeSize;
            var chuckCenterMachinePosition = cacheProvider.GetOrDefault<ChuckCenterObjDto>();

            var maskWaferPosition = maskDto.MaskWaferCellPosition +
                                    (Vector)(reticleBuilder.OriginalDiePoint - (Vector)new Point(0, diePitchHeight + scribeSize.Height));

            var maskMachinePosition = chuckCenterMachinePosition.NewBFCenterStagePosition +
                                      (Vector)new Point(_stageDirection.X * maskWaferPosition.X, _stageDirection.Y * maskWaferPosition.Y);

            if (IsReview)
            {
                var offset = applicationCookie.CalibrationReviseRecipeDto!.WaferDto.WaferCenterWaferPosition
                             - (Vector)applicationCookie.CalibrationRecipeDto!.WaferDto.WaferCenterWaferPosition!;
                maskMachinePosition += (Vector)new Point(_stageDirection.X * offset!.Value.X, _stageDirection.Y * offset.Value.Y);
            }

            StageViewModel.SetMachineAbsoluteStageXy(maskMachinePosition);
        }
        catch (Exception ex)
        {
            dialogWindowProvider.ShowDialog("Set Machine Absolute Stage Xy failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            logger.LogError(ex, "Set Machine Absolute Stage Xy failed!");
        }
    }

    [RelayCommand]
    private void LoadBrightImageFilePath(object? obj)
    {
        try
        {
            if (obj is null)
                return;

            var (maskDto, _) = GetSelectReticleMaskListInfo(obj.ToString());

            var dialog = dialogWindowProvider.TryShowSelectFilePathDialog(".jpg", out var filePath);
            if (dialog == false) return;

            maskDto.RecipeBrightFieldTemplateDto.TemplateImageFilePath = filePath;
            maskDto.RecipeBrightFieldTemplateDto.TemplateFilePath = FileHelper.GetFileFullName(filePath);
        }
        catch (Exception ex)
        {
            dialogWindowProvider.ShowDialog("Load Template Image File Path Failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            logger.LogError(ex, "Load Template Image File Path Failed!");
        }
    }

    [RelayCommand]
    private void LoadDarkImageFilePath(object? obj)
    {
        try
        {
            if (obj is null)
                return;

            var (maskDto, _) = GetSelectReticleMaskListInfo(obj.ToString());

            var dialog = dialogWindowProvider.TryShowSelectFilePathDialog(".jpg", out var filePath);
            if (dialog == false) return;

            maskDto.RecipeDarkFieldTemplateDto.TemplateImageFilePath = filePath;
            maskDto.RecipeDarkFieldTemplateDto.TemplateFilePath = FileHelper.GetFileFullName(filePath);
        }
        catch (Exception ex)
        {
            dialogWindowProvider.ShowDialog("Load Template Image File Path Failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            logger.LogError(ex, "Load Template Image File Path Failed!");
        }
    }

    [RelayCommand]
    private async Task GenerateBrightTemplateAsync(object? obj)
    {
        try
        {
            if (obj is null)
                return;
            if (SelectReticleMarkItem is null)
            {
                dialogWindowProvider.ShowDialog("Select Reticle Item Is Empty!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return;
            }

            var (maskDto, directoryName) = GetSelectReticleMaskListInfo(obj.ToString());

            maskDto.RecipeBrightFieldTemplateDto.TemplateFilePath = $"{TemplateFileDirectory}\\{directoryName}\\BrightField\\Ncc\\{maskDto.Remark}_{maskDto.ReticleMaskTypeEnum}_{maskDto.RecipeBrightFieldTemplateDto.MicroscopeLensInformation.LensName}_{Guid.NewGuid()}";
            var templateFilePath = maskDto.RecipeBrightFieldTemplateDto.TemplateFilePath;

            microscopeViewModel.SwitchMicroscopeLensInformation(maskDto.RecipeBrightFieldTemplateDto.MicroscopeLensInformation);
            await Task.Delay(3000);
            var generateTemplate = reviewViewModel.TryGenerateTemplate(AlgorithmTemplateTypeEnum.Ncc, maskDto.RecipeBrightFieldTemplateDto.TemplateFilePath, AlgorithmTemplateSizeEnum.Size256);
            if (generateTemplate == false)
            {
                dialogWindowProvider.ShowDialog("Generate Template Failed", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return;
            }

            maskDto.RecipeBrightFieldTemplateDto.TemplateImageFilePath = CalibrationConstantsHelper.TemplatePathToTemplateImagePath(templateFilePath);
        }
        catch (Exception ex)
        {
            dialogWindowProvider.ShowDialog("Generate Bright Field Template Failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            logger.LogError(ex, "Generate Bright Field Template Failed!");
        }
    }

    [RelayCommand]
    private void GenerateDarkTemplate(object? obj)
    {
        try
        {
            if (obj is null)
                return;

            var (maskDto, directoryName) = GetSelectReticleMaskListInfo(obj.ToString());

            var brightPosition = StageViewModel.GetBrightFieldStagePosition();
            var darkFieldImageDto = laserViewModel.GetDarkFieldLineScanImage(
                CalChipSiteModelEnum.ChuckModel,
                brightPosition,
                (false, 0.85),
                false,
                CalibrationRecipeDto.CalibrationRecipeInfoDto.CIBConfiguration,
                800,
                maskDto.RecipeDarkFieldTemplateDto.OpticsMagTypeEnum,
                maskDto.RecipeDarkFieldTemplateDto.StageSpeedEnum,
                stageCoordinateSystemEnum: StageCoordinateSystemEnum.Bright);
            using var _ = darkFieldImageDto;

            var templateFilePath = $"{TemplateFileDirectory}\\{directoryName}\\DarkField\\Ncc\\{maskDto.Remark}_{maskDto.ReticleMaskTypeEnum}_{maskDto.RecipeDarkFieldTemplateDto.OpticsMagTypeEnum}_{Guid.NewGuid()}";
            var templateImageFilePath = CalibrationConstantsHelper.TemplatePathToTemplateImagePath(templateFilePath);

            HalconHelper.Save(darkFieldImageDto.Image, templateImageFilePath);

            createDarkImageTemplateWindowViewModel.ImageFilePath = templateImageFilePath;
            createDarkImageTemplateWindowViewModel.TemplateFilePath = templateFilePath;
            var showDialog = windowManagerService.ShowDialog(createDarkImageTemplateWindowViewModel);

            if (showDialog == false)
            {
                dialogWindowProvider.ShowDialog("Generate Template Failed", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return;
            }

            maskDto.RecipeDarkFieldTemplateDto.AlgorithmTemplateTypeEnum = createDarkImageTemplateWindowViewModel.AlgorithmTemplateTypeEnum;
            maskDto.RecipeDarkFieldTemplateDto.AlgorithmTemplateSizeEnum = createDarkImageTemplateWindowViewModel.AlgorithmTemplateSizeEnum;
            maskDto.RecipeDarkFieldTemplateDto.TemplateFilePath = createDarkImageTemplateWindowViewModel.TemplateFilePath;
            maskDto.RecipeDarkFieldTemplateDto.TemplateImageFilePath = createDarkImageTemplateWindowViewModel.TemplateImageFilePath;
        }
        catch (Exception ex)
        {
            dialogWindowProvider.ShowDialog("Generate Dark Field Template Failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            logger.LogError(ex, "Generate Dark Field Template Failed!");
        }
    }

    private (ReticleMarkItemDto selectItem, string calibrationTypeName) GetSelectReticleMaskListInfo(string? name)
    {
        if (SelectReticleMarkItem is null)
        {
            dialogWindowProvider.ShowDialog("Select Reticle Item Is Empty!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            throw new NullReferenceException(nameof(SelectReticleMarkItem));
        }

        ReticleMarkItemDto maskDto;
        string calibrationTypeName;
        switch (name)
        {
            case nameof(CalibrationRecipeDto.WaferDto.ReticleMarkDto.MicrosocpeReticleMarkItemList):
                var selectItem = CalibrationRecipeDto.WaferDto.ReticleMarkDto.MicrosocpeReticleMarkItemList.Index().FirstOrDefault(t => SelectReticleMarkItem.MaskIndex == t.Value.MaskIndex);
                EditReticleMarkList = CalibrationRecipeDto.WaferDto.ReticleMarkDto.MicrosocpeReticleMarkItemList;
                maskDto = EditReticleMarkList[selectItem.Key];
                calibrationTypeName = "Microscope";
                break;

            case nameof(CalibrationRecipeDto.WaferDto.ReticleMarkDto.ChuckReticleMarkItemList):
                selectItem = CalibrationRecipeDto.WaferDto.ReticleMarkDto.ChuckReticleMarkItemList.Index().FirstOrDefault(t => SelectReticleMarkItem.MaskIndex == t.Value.MaskIndex);
                EditReticleMarkList = CalibrationRecipeDto.WaferDto.ReticleMarkDto.ChuckReticleMarkItemList;
                maskDto = EditReticleMarkList[selectItem.Key];
                calibrationTypeName = "Chuck";
                break;

            case nameof(CalibrationRecipeDto.WaferDto.ReticleMarkDto.LaserReticleMarkItemList):
                selectItem = CalibrationRecipeDto.WaferDto.ReticleMarkDto.LaserReticleMarkItemList.Index().FirstOrDefault(t => SelectReticleMarkItem.MaskIndex == t.Value.MaskIndex);
                EditReticleMarkList = CalibrationRecipeDto.WaferDto.ReticleMarkDto.LaserReticleMarkItemList;
                maskDto = EditReticleMarkList[selectItem.Key];
                calibrationTypeName = "Laser";
                break;

            default:
                throw new ArgumentException("Invalid object type", nameof(name));
        }

        if (maskDto is null)
        {
            throw new NullReferenceException(name);
        }

        return (maskDto, calibrationTypeName);
    }

    #endregion Template Command

    #region 刷新界面

    [RelayCommand]
    private void ParamTypeChanged(object obj)
    {
        if (obj is MouseButtonEventArgs e)
        {
            e.Handled = true; // 阻止冒泡
        }

        if (EditRecipeTypeName.Contains("Alignment"))
            AlignmentWindowDarkFieldViewModel.LoadedCommand.Execute(null);
        else
            NotifyWaferMapView();
    }

    [RelayCommand]
    private void MaskTypeChanged(object obj) => RefreshReticleView(obj);

    [RelayCommand]
    private void Loading(object obj) => RefreshReticleView(obj);

    private void RefreshReticleView(object obj)
    {
        if (obj is not TabItem tabItem)
            return;

        var contentControl = (tabItem.Content as ContentControl)!;

        EditReticleMarkList = (contentControl.Content as ObservableCollection<ReticleMarkItemDto>)!;
        if (EditReticleMarkList is null)
            return;

        RefreshReticleMaskView();
    }

    private void RefreshReticleMaskView()
    {
        ReticleMarkList = [.. EditReticleMarkList];
    }

    #endregion 刷新界面

    [RelayCommand]
    private void Debug()
    {
        var waferCenterBrightFieldPosition = CalibrationRecipeDto.WaferDto.WaferCenterWaferPosition!.Value;
        var originReticleWaferPosition = WaferMapCanvasViewModel.Document.ReticleBuilder.OriginalDiePoint;
        // 对准缓存
        var alignmentCacheBrightField = recipeCacheProvider.GetOrDefault<AlignmentCacheBrightField>();

        // 重新对准
        var alignmentResult = StageViewModel.Alignment(alignmentCacheBrightField.LowSite1, alignmentCacheBrightField.LowSite2,
            alignmentCacheBrightField.HighSite1, alignmentCacheBrightField.HighSite2,
            alignmentCacheBrightField.LowMag, alignmentCacheBrightField.HighMag,
            alignmentCacheBrightField.AlgorithmWaferTypeEnum);
        // 配方对准结果缓存
        var recipeAlignmentResult = CalibrationRecipeDto.WaferDto.AlignmentResultDto;

        // 当前对准与配方对准结果的偏移量（waferMap偏移值）
        var offsetX = ((alignmentResult.MarkPoint1.X - recipeAlignmentResult!.MarkPoint1.X) +
                       (alignmentResult.MarkPoint2.X - recipeAlignmentResult.MarkPoint2.X)) / 2;
        var offsetY = ((alignmentResult.MarkPoint1.Y - recipeAlignmentResult.MarkPoint1.Y) +
                       (alignmentResult.MarkPoint2.Y - recipeAlignmentResult.MarkPoint2.Y)) / 2;
        var offsetPosition = new Point(offsetX, offsetY);

        // 重新对准后的originDie Corner位置
        StageViewModel.SetBrightFieldAbsoluteStageXy(originReticleWaferPosition);
        var ideaOriginReticleBrightPosition = originReticleWaferPosition
                                              + (Vector)waferCenterBrightFieldPosition
                                              + (Vector)offsetPosition;
        StageViewModel.SetBrightFieldAbsoluteStageXy(ideaOriginReticleBrightPosition);

        // 重新对准后的指定索引Reticle Die Corner位置
        var ideaReticleDieCornerWaferPosition = WaferMapCanvasViewModel.Document.ReticleModel.Single(t => t.Index is { X: 4, Y: 4 });
        var realReticleDieCornerBrightFieldPosition = ideaReticleDieCornerWaferPosition.Rect.Point
                                                      + (Vector)waferCenterBrightFieldPosition
                                                      + (Vector)offsetPosition;
        StageViewModel.SetBrightFieldAbsoluteStageXy(realReticleDieCornerBrightFieldPosition);

        // 重新对准后基于OriginDieCorner为基准的ReticleMask位置
        var reticleBuilder = WaferMapCanvasViewModel.Document.ReticleBuilder;

        var reticleHeight = reticleBuilder.DiePitchSize.Height;
        var ideaReticleMaskPosition = SelectReticleMarkItem!.MaskWaferCellPosition;
        var realReticleMaskBrightFieldPosition = ideaReticleMaskPosition + (Vector)offsetPosition
                                                                         + (Vector)waferCenterBrightFieldPosition
                                                                         + (originReticleWaferPosition - new Point(0, reticleHeight));
        microscopeViewModel.SwitchMicroscopeLensInformation(SelectReticleMarkItem!.RecipeBrightFieldTemplateDto.MicroscopeLensInformation);
        StageViewModel.SetBrightFieldAbsoluteStageXy(realReticleMaskBrightFieldPosition);

        // 重新对准后基于指定索引的ReticleDieCorner为基准的Mask位置
        var maskWaferPosition = ideaReticleMaskPosition +
                                (ideaReticleDieCornerWaferPosition.Rect.Point - new Point(0, reticleHeight));
        var realMaskBrightFieldPosition = maskWaferPosition + (Vector)offsetPosition + (Vector)waferCenterBrightFieldPosition;
        StageViewModel.SetBrightFieldAbsoluteStageXy(realMaskBrightFieldPosition);
    }

    [RelayCommand]
    private void Verify()
    {
        if (calibrationRecipeService.GetCorrectWaferMapByOffset(true) == false)
            return;
        var reviseRecipeDto = applicationCookie.CalibrationReviseRecipeDto;

        microscopeViewModel.SwitchMicroscopeLensInformation(CalibrationRecipeDto.CalibrationRecipeInfoDto.MicroscopeHighMag);

        // 重新对准后的originDie Corner位置
        var originDieDto = reviseRecipeDto!.WaferDto.WaferMapCanvasDocument.DieBuilder.OriginalDiePoint;
        StageViewModel.SetBrightFieldAbsoluteStageXy(originDieDto);

        // 重新对准后的reticleDie corner位置
        var originReticleDto = reviseRecipeDto.WaferDto.WaferMapCanvasDocument.ReticleBuilder.OriginalDiePoint;
        StageViewModel.SetBrightFieldAbsoluteStageXy(originReticleDto);

        // 重新对准后的指定索引Die Corner位置
        var diePitchDto = reviseRecipeDto.WaferDto.WaferMapCanvasDocument.DieModel.Single(t => t.Index is { X: 12, Y: 4 });
        StageViewModel.SetBrightFieldAbsoluteStageXy(diePitchDto.Rect.Point);

        // 重新对准后的指定索引Reticle Die Corner位置
        var reticleDto = reviseRecipeDto.WaferDto.WaferMapCanvasDocument.ReticleModel.Single(t => t.Index is { X: 10, Y: 4 });
        StageViewModel.SetBrightFieldAbsoluteStageXy(reticleDto.Rect.Point);

        // 重新对准后基于OriginDieCorner为基准的ReticleMask位置
        var originDie = reviseRecipeDto.WaferDto.WaferMapCanvasDocument.DieModel.Single(t => t.Index is { X: 0, Y: 0 });
        calibrationRecipeService.GetMicroscopeReticleMaskInfo(SelectReticleMarkItem!.ReticleMaskTypeEnum, SelectReticleMarkItem!.RecipeBrightFieldTemplateDto.MicroscopeLensInformation, null, out var maskDto);
        calibrationRecipeService.GetDieMaskBrightFieldPosition(originDie, maskDto, out var originReticleMaskBrightFieldPosition);

        microscopeViewModel.SwitchMicroscopeLensInformation(SelectReticleMarkItem!.RecipeBrightFieldTemplateDto.MicroscopeLensInformation);
        StageViewModel.SetBrightFieldAbsoluteStageXy(originReticleMaskBrightFieldPosition);

        // 重新对准后基于指定索引的ReticleDieCorner为基准的Mask位置
        calibrationRecipeService.GetReticleMaskBrightFieldPosition(reticleDto, maskDto, out var reticleMaskBrightFieldPosition);
        StageViewModel.SetBrightFieldAbsoluteStageXy(reticleMaskBrightFieldPosition);

        var microscopePixelSizeItems = cacheProvider.GetArray<MicroscopePixelSizeItemDto>();
        var template = SelectReticleMarkItem!.RecipeBrightFieldTemplateDto;
        //var appHomeDirectory = HostApplication.GetRequiredService<IOptions<ApplicationSetting>>().Value.AppHomeDirectory;
        if (reviewViewModel.TryGetMatchPosition(template.AlgorithmTemplateTypeEnum, microscopePixelSizeItems!, reticleMaskBrightFieldPosition, template.MicroscopeLensInformation,
                template.TemplateFilePath, Path.GetDirectoryName(template.TemplateImageFilePath), null, null,
                "Magnification", out _, out _, out _, out _, out _) == false) return;
    }

    #endregion Reticle Mask
}
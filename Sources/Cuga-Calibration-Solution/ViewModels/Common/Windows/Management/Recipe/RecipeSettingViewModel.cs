using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Core.Models.Enums.Algorithm;
using Core.Models.Enums.Microscope;
using Core.Models.Enums.Stage;
using Core.Models.Events;
using Core.Models.Helper;
using Core.Models.Models.Common.Alignment;
using Core.Models.Models.Common.Recipe;
using Core.Models.Models.Common.Recipe.Wafer.ReticleMask;
using Core.Models.Models.Microscope.PixelSize;
using CugaCalibration.Core.Models;
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
using Net.Utilities.Algorithm.Halcon.Helper;
using Net.Utilities.Attributes;
using Net.Utilities.Constants;
using Net.Utilities.Enums;
using Net.Utilities.Helper.File;
using Net.Utilities.Helper.IOC.Providers;
using Net.Utilities.Models;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.Services;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Controls;
using System.Windows.Input;

namespace CugaCalibration.ViewModels.Common.Windows.Management.Recipe;

[IOCAppService(ServiceType = typeof(RecipeSettingViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class RecipeSettingViewModel(
    ICacheProvider cacheProvider,
    [FromKeyedServices(LiteDbConstantHelper.RecipeDbKey)] ICacheProvider recipeCacheProvider,
    ILogger<RecipeSettingViewModel> logger,
    IDialogWindowProvider dialogWindowProvider,
    IWindowManagerService windowManagerService,
    IMessenger messenger,
    ISynchronizationContextProvider synchronizationContext,
    ICalibrationRecipeService calibrationRecipeService,
    IOptions<ApplicationSetting> options,
    ISysRecipeInformationService sysRecipeInformationService,
    [FromKeyedServices(LiteDbConstantHelper.RecipeDbKey)] ILiteDatabaseProvider liteDatabaseProvider,
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

    #region 字段

    /// <summary>
    /// 模板存储位置
    /// </summary>
    private string TemplateFileDirectory => Path.Combine(_appHomeDirectory, "Template", "Recipe", EditRecipeTypeName, DateTime.Now.ToString(ConstantHelper.ShortFileDateTimeFormat));

    #endregion 字段

    #region 界面显示

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

    #region 业务

    [RelayCommand]
    private async Task LoadedAsync()
    {
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
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
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
    }

    #endregion 业务

    #region wafer Map

    [RelayCommand]
    private Task RefreshWaferMapAsync(object? name)
    {
        return Task.Run(() =>
        {
            if (name is null) return;
            IsReticleMode = name.ToString() == "Reticle";
            var magnificationEnum = microscopeViewModel.GetMagnification();
            if (magnificationEnum != MicroscopeMagnificationEnum.Magnification50X)
            {
                dialogWindowProvider.ShowDialog("The generation wafermap must to be done under a 50x lens. Please re-obtain the origin die coordinates ", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                microscopeViewModel.SwitchMagnification(MicroscopeMagnificationEnum.Magnification50X);
            }

            if (GenerateWaferMap() == false)
                dialogWindowProvider.ShowDialog("Generate Wafer Map Failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
        });
    }

    private bool GenerateWaferMap()
    {
        try
        {
            var originDieBrightPosition = StageViewModel.GetBrightFieldStagePosition();

            CalibrationRecipeDto.WaferDto.WaferMapDto.WaferMapInitialization(originDieBrightPosition - CalibrationRecipeDto.WaferDto.WaferCenterBrightFieldPosition!.Value);
            CalibrationRecipeDto.WaferDto.WaferMapDto.GenerateMapByOriginDie();

            if (SelectRecipeDtoBackup!.WaferDto.WaferMapDto.WaferMapData.Equals(CalibrationRecipeDto.WaferDto.WaferMapDto.WaferMapData) == false)
                dialogWindowProvider.ShowDialog("Wafer map data changed! Please re-obtain the reticle cell position of the mask", DialogButtonsEnum.OK, DialogIconEnum.Warning);

            NotifyWaferMapView();
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
        OnPropertyChanged(nameof(CalibrationRecipeDto));
    }

    #endregion wafer Map

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

    [RelayCommand]
    private void GetMaskReticlePosition(object? obj)
    {
        try
        {
            if (obj is null)
                return;
            var (maskDto, _) = GetSelectReticleMaskListInfo(obj.ToString());
            var waferMapData = CalibrationRecipeDto.WaferDto.WaferMapDto.WaferMapData;
            var brightPosition = StageViewModel.GetBrightFieldStagePosition();
            if (brightPosition.DistanceToZero() >= waferMapData.WaferDiameter / 2)
            {
                dialogWindowProvider.ShowDialog("The position out of the wafer range!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return;
            }

            var waferPosition = brightPosition - CalibrationRecipeDto.WaferDto.WaferCenterBrightFieldPosition!.Value;

            var reticleHeight = waferMapData.ReticleHeight;

            var relativeReticleOriginPosition = waferPosition
                                                - (CalibrationRecipeDto.WaferDto.WaferMapDto.OriginReticleDto.WaferPosition - new Point(0, reticleHeight));

            maskDto.MaskWaferCellPosition = relativeReticleOriginPosition;
            RefreshReticleMaskView();
        }
        catch (Exception ex)
        {
            dialogWindowProvider.ShowDialog("Get Mask Reticle Position Failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            logger.LogError(ex, "Get Mask Reticle Position Failed!");
        }
    }

    [RelayCommand]
    private void GotoMaskReticlePosition(object? obj)
    {
        try
        {
            if (obj is null)
                return;
            var (maskDto, _) = GetSelectReticleMaskListInfo(obj.ToString());
            microscopeViewModel.SwitchMagnification(maskDto.RecipeBrightFieldTemplateDto.MicroscopeMagnificationEnum);

            var reticleHeight = CalibrationRecipeDto.WaferDto.WaferMapDto.WaferMapData.ReticleHeight;

            var maskWaferPosition = maskDto.MaskWaferCellPosition +
                                    (CalibrationRecipeDto.WaferDto.WaferMapDto.OriginReticleDto.WaferPosition - new Point(0, reticleHeight));

            StageViewModel.SetBrightFieldAbsoluteStageXy(maskWaferPosition + CalibrationRecipeDto.WaferDto.WaferCenterBrightFieldPosition!.Value);
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
    private async void GenerateBrightTemplate(object? obj)
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

            maskDto.RecipeBrightFieldTemplateDto.TemplateFilePath = $"{TemplateFileDirectory}\\{directoryName}\\BrightField\\Ncc\\{maskDto.Remark}_{maskDto.RecipeDarkFieldTemplateDto.WaferMaskTypeEnum}_{maskDto.RecipeBrightFieldTemplateDto.MicroscopeMagnificationEnum}_{Guid.NewGuid()}";
            var templateFilePath = maskDto.RecipeBrightFieldTemplateDto.TemplateFilePath;

            microscopeViewModel.SwitchMagnification(maskDto.RecipeBrightFieldTemplateDto.MicroscopeMagnificationEnum);
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
                null,
                800,
                maskDto.RecipeDarkFieldTemplateDto.OpticsMagTypeEnum,
                maskDto.RecipeDarkFieldTemplateDto.StageSpeedEnum,
                stageCoordinateSystemEnum: StageCoordinateSystemEnum.Bright);
            using var _ = darkFieldImageDto;

            var templateFilePath = $"{TemplateFileDirectory}\\{directoryName}\\DarkField\\Ncc\\{maskDto.Remark}_{maskDto.RecipeDarkFieldTemplateDto.WaferMaskTypeEnum}_{maskDto.RecipeDarkFieldTemplateDto.OpticsMagTypeEnum}_{Guid.NewGuid()}";
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

    private (ReticleMarkItemDto selectItem, string calibrationTypeName) GetSelectReticleMaskListInfo(string name)
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
        var waferCenterBrightFieldPosition = CalibrationRecipeDto.WaferDto.WaferCenterBrightFieldPosition!.Value;
        var originReticleWaferPosition = CalibrationRecipeDto.WaferDto.WaferMapDto.OriginReticleDto.WaferPosition;
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
                                              + waferCenterBrightFieldPosition
                                              + offsetPosition;
        StageViewModel.SetBrightFieldAbsoluteStageXy(ideaOriginReticleBrightPosition);

        // 重新对准后的指定索引Reticle Die Corner位置
        var ideaReticleDieCornerWaferPosition = CalibrationRecipeDto.WaferDto.WaferMapDto.WaferMapReticleDieDtoItemList[4][4];
        var realReticleDieCornerBrightFieldPosition = ideaReticleDieCornerWaferPosition.WaferPosition
                                                      + waferCenterBrightFieldPosition
                                                      + offsetPosition;
        StageViewModel.SetBrightFieldAbsoluteStageXy(realReticleDieCornerBrightFieldPosition);

        // 重新对准后基于OriginDieCorner为基准的ReticleMask位置
        var reticleHeight = CalibrationRecipeDto.WaferDto.WaferMapDto.WaferMapData.ReticleHeight;
        var ideaReticleMaskPosition = SelectReticleMarkItem!.MaskWaferCellPosition;
        var realReticleMaskBrightFieldPosition = ideaReticleMaskPosition + offsetPosition
                                                                         + waferCenterBrightFieldPosition
                                                                         + (originReticleWaferPosition - new Point(0, reticleHeight));
        microscopeViewModel.SwitchMagnification(SelectReticleMarkItem!.RecipeBrightFieldTemplateDto.MicroscopeMagnificationEnum);
        StageViewModel.SetBrightFieldAbsoluteStageXy(realReticleMaskBrightFieldPosition);

        // 重新对准后基于指定索引的ReticleDieCorner为基准的Mask位置
        var maskWaferPosition = ideaReticleMaskPosition +
                                (ideaReticleDieCornerWaferPosition.WaferPosition - new Point(0, reticleHeight));
        var realMaskBrightFieldPosition = maskWaferPosition + offsetPosition + waferCenterBrightFieldPosition;
        StageViewModel.SetBrightFieldAbsoluteStageXy(realMaskBrightFieldPosition);
    }

    [RelayCommand]
    private void Verify()
    {
        if (calibrationRecipeService.GetCorrectWaferMapByOffset(true) == false)
            return;
        var reviseRecipeDto = applicationCookie.CalibrationReviseRecipeDto;

        microscopeViewModel.SwitchMagnification(MicroscopeMagnificationEnum.Magnification50X);

        // 重新对准后的originDie Corner位置
        var originDieDto = reviseRecipeDto!.WaferDto.WaferMapDto.OriginDieDto;
        StageViewModel.SetBrightFieldAbsoluteStageXy(originDieDto.WaferPosition);

        // 重新对准后的reticleDie corner位置
        var originReticleDto = reviseRecipeDto.WaferDto.WaferMapDto.OriginReticleDto;
        StageViewModel.SetBrightFieldAbsoluteStageXy(originReticleDto.WaferPosition);

        // 重新对准后的指定索引Die Corner位置
        var diePitchDto = reviseRecipeDto.WaferDto.WaferMapDto.WaferMapDieDtoItemList[12][44];
        StageViewModel.SetBrightFieldAbsoluteStageXy(diePitchDto.WaferPosition);

        // 重新对准后的指定索引Reticle Die Corner位置
        var reticleDto = reviseRecipeDto.WaferDto.WaferMapDto.WaferMapReticleDieDtoItemList[10][4];
        StageViewModel.SetBrightFieldAbsoluteStageXy(reticleDto.WaferPosition);

        // 重新对准后基于OriginDieCorner为基准的ReticleMask位置
        calibrationRecipeService.GetMicroscopeReticleMaskInfo(SelectReticleMarkItem!.ReticleMaskTypeEnum, SelectReticleMarkItem!.RecipeBrightFieldTemplateDto.MicroscopeMagnificationEnum, null, out var maskDto);
        calibrationRecipeService.GetReticleMaskBrightFieldPosition(originReticleDto, maskDto, out var originReticleMaskBrightFieldPosition);

        microscopeViewModel.SwitchMagnification(SelectReticleMarkItem!.RecipeBrightFieldTemplateDto.MicroscopeMagnificationEnum);
        StageViewModel.SetBrightFieldAbsoluteStageXy(originReticleMaskBrightFieldPosition);

        // 重新对准后基于指定索引的ReticleDieCorner为基准的Mask位置
        calibrationRecipeService.GetReticleMaskBrightFieldPosition(reticleDto, maskDto, out var reticleMaskBrightFieldPosition);
        StageViewModel.SetBrightFieldAbsoluteStageXy(reticleMaskBrightFieldPosition);

        var microscopePixelSizeItems = cacheProvider.GetArray<MicroscopePixelSizeItemDto>();
        var template = SelectReticleMarkItem!.RecipeBrightFieldTemplateDto;
        //var appHomeDirectory = HostApplication.GetRequiredService<IOptions<ApplicationSetting>>().Value.AppHomeDirectory;
        if (reviewViewModel.TryGetMatchPosition(template.AlgorithmTemplateTypeEnum, microscopePixelSizeItems!, reticleMaskBrightFieldPosition, template.MicroscopeMagnificationEnum,
                template.TemplateFilePath, Path.GetDirectoryName(template.TemplateImageFilePath), null, null,
                "Magnification", out _, out _, out _, out _, out _) == false) return;
    }

    #endregion Reticle Mask
}
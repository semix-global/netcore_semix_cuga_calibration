using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Algorithm;
using Core.Models.Enums.Stage;
using Core.Models.Helper;
using Core.Models.Models.Chuck.CenterAndTheta;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Setting;
using Core.Recipe.Models;
using Core.Recipe.Models.Wafer.ReticleMask;
using CugaCalibration.ViewModels.Common.Windows.Tools;
using CugaCalibration.ViewModels.Common.Windows.Tools.Alignment;
using Local.SQL.Cache.Providers.Extensions;
using Local.SQL.Cache.Providers.Interfaces;
using Microsoft.Extensions.Logging;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.IOC.Providers;
using Net.Utilities.Models.Geometries;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.Services;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows.Input;

namespace CugaCalibration.ViewModels.Common.Windows.Recipe.Edit.Children;

/// <summary>
/// 职责：ReticleMask 增删 / 模板生成 / 位置获取
/// </summary>
[IOCAppService(ServiceType = typeof(RecipeReticleMaskViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class RecipeReticleMaskViewModel(
    ICacheProvider cacheProvider,
    ILogger<RecipeReticleMaskViewModel> logger,
    IDialogWindowProvider dialogWindowProvider,
    IWindowManagerService windowManagerService,
    ISynchronizationContextProvider contextProvider,
    MicroscopeViewModel microscopeViewModel,
    ReviewViewModel reviewViewModel,
    CIBViewModel cibViewModel,
    StageViewModel stageViewModel,
    CalibrationSetting calibrationSetting,
    RecipeCookie recipeCookie,
    ApplicationCookie applicationCookie,
    CreateDarkImageTemplateWindowViewModel createDarkImageTemplateWindowViewModel,
    AlignmentWindowDarkFieldViewModel alignmentWindowDarkFieldViewModel) : ViewModelBase
{
    public ApplicationCookie ApplicationCookie { get; } = applicationCookie;
    public AlignmentWindowDarkFieldViewModel AlignmentWindowDarkFieldViewModel { get; } = alignmentWindowDarkFieldViewModel;

    [ObservableProperty]
    private ReticleMarkItemDto? _selectReticleMarkItem;

    /// <summary>
    /// 当前 Tab 对应的列表（编辑用）
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<ReticleMarkItemDto> _editReticleMarkList = [];

    /// <summary>
    /// 用于 ReticleMaskView 绘制的镜像列表
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<ReticleMarkItemDto> _reticleMarkList = [];

    // 内部引用（由主 VM 注入）

    /// <summary>
    /// 当前编辑中的配方
    /// </summary>
    public CalibrationRecipeDTO? EditingDto { get; set; }

    /// <summary>
    /// 模板目录前缀
    /// </summary>
    public string TemplateFileDirectory { get; set; } = string.Empty;

    /// <summary>
    /// 当前编辑 Tab 名称
    /// </summary>
    public string EditRecipeTypeName { get; set; } = string.Empty;

    /// <summary>
    /// WaferMap 子 VM（用于获取 Builder 信息）
    /// </summary>
    public RecipeWaferMapViewModel? WaferMapSubViewModel { get; set; }

    public void NotifyAll()
    {
        contextProvider.Post(() => { OnPropertyChanged(nameof(EditingDto)); });
    }

    [RelayCommand]
    public void ParamTypeChanged(object obj)
    {
        if (obj is MouseButtonEventArgs e)
            e.Handled = true;

        if (EditRecipeTypeName.Contains("Wafer")) WaferMapSubViewModel?.NotifyWaferMapView();
    }

    [RelayCommand]
    public void MaskTypeChanged(object obj) => RefreshReticleView(obj);

    [RelayCommand]
    public void Loading(object obj) => RefreshReticleView(obj);

    private void RefreshReticleView(object obj)
    {
        if (EditingDto is null) return;

        var header = obj switch
        {
            TabItem tabItem => tabItem.Header?.ToString(),
            string s => s,
            _ => null
        };

        if (header is null) return;

        var listName = header switch
        {
            "Microscope" => nameof(ReticleMarkDto.MicrosocpeReticleMarkItemList),
            "Chuck" => nameof(ReticleMarkDto.ChuckReticleMarkItemList),
            "Laser" => nameof(ReticleMarkDto.LaserReticleMarkItemList),
            _ => null
        };

        if (listName is null) return;

        EditReticleMarkList = GetReticleMaskConfigList(listName, EditingDto);
        EditRecipeTypeName = header;
        RefreshReticleMaskView();
    }

    private void RefreshReticleMaskView() => ReticleMarkList = [.. EditReticleMarkList];

    [RelayCommand]
    private void AddMask()
    {
        if (EditingDto is null) return;
        var list = EditReticleMarkList;
        contextProvider.Send(() => list.Add(new ReticleMarkItemDto
        {
            MaskIndex = list.Count != 0 ? list.Last().MaskIndex + 1 : 0
        }));
    }

    [RelayCommand]
    private void DeleteMask()
    {
        if (SelectReticleMarkItem is null || EditingDto is null) return;
        var list = EditReticleMarkList;
        contextProvider.Send(() =>
        {
            for (var i = SelectReticleMarkItem.MaskIndex + 1; i < list.Count; i++)
                list[i].MaskIndex -= 1;
            list.Remove(SelectReticleMarkItem);
        });
    }

    [RelayCommand]
    private void GetMaskReticlePosition()
    {
        try
        {
            if (WaferMapSubViewModel is null) return;
            var maskDto = GetSelectReticleMaskItem();
            var waferBuilder = WaferMapSubViewModel.WaferMapCanvasViewModel.Document.WaferBuilder;
            var reticleBuilder = WaferMapSubViewModel.WaferMapCanvasViewModel.Document.ReticleBuilder;
            var machinePosition = stageViewModel.GetMachineStagePosition();

            if (machinePosition.ToOriginLength >= waferBuilder.Circle.Radius)
            {
                dialogWindowProvider.ShowDialog("The position out of the wafer range!",
                    DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return;
            }

            var chuckCenter = cacheProvider.GetOrDefault<ChuckCenterAndThetaItemDto>();
            var waferPosition = machinePosition - (Vector)chuckCenter.NewBFCenterStagePosition;
            var dir = WaferMapSubViewModel.StageDirection;
            var relativeReticleOriginPosition =
                new Point(dir.X * waferPosition.X, dir.Y * waferPosition.Y)
                - (Vector)(reticleBuilder.OriginalDiePoint - (Vector)new Point(0, reticleBuilder.DiePitchSize.Height + reticleBuilder.DieScribeSize.Height));

            maskDto.MaskWaferCellPosition = relativeReticleOriginPosition;
            RefreshReticleMaskView();
        }
        catch (Exception ex)
        {
            dialogWindowProvider.ShowDialog("Get Mask Reticle Position Failed!",
                DialogButtonsEnum.OK, DialogIconEnum.Warning);
            logger.LogError(ex, "Get Mask Reticle Position Failed!");
        }
    }

    [RelayCommand]
    private void GotoMaskReticlePosition()
    {
        try
        {
            if (WaferMapSubViewModel is null) return;
            var maskDto = GetSelectReticleMaskItem();
            microscopeViewModel.SwitchMicroscopeLensInformation(maskDto.RecipeBrightFieldTemplateDto.MicroscopeLensInformation);

            var reticleBuilder = WaferMapSubViewModel.WaferMapCanvasViewModel.Document.ReticleBuilder;
            var dir = WaferMapSubViewModel.StageDirection;
            var chuckCenter = cacheProvider.GetOrDefault<ChuckCenterAndThetaItemDto>();

            var maskWaferPosition = maskDto.MaskWaferCellPosition
                                    + (Vector)(reticleBuilder.OriginalDiePoint - (Vector)new Point(0, reticleBuilder.DiePitchSize.Height + reticleBuilder.DieScribeSize.Height));

            var maskMachinePosition = chuckCenter.NewBFCenterStagePosition
                                      + (Vector)new Point(dir.X * maskWaferPosition.X, dir.Y * maskWaferPosition.Y);

            if (recipeCookie.CalibrationReviseRecipeDto is not null)
            {
                var offset = recipeCookie.CalibrationReviseRecipeDto.WaferDto.WaferCenterWaferPosition
                             - (Vector)recipeCookie.CalibrationRecipeDto!.WaferDto.WaferCenterWaferPosition!;
                if (offset.HasValue)
                    maskMachinePosition += (Vector)new Point(dir.X * offset.Value.X, dir.Y * offset.Value.Y);
            }

            stageViewModel.SetMachineAbsoluteStageXy(maskMachinePosition);
        }
        catch (Exception ex)
        {
            dialogWindowProvider.ShowDialog("Set Machine Absolute Stage Xy failed!",
                DialogButtonsEnum.OK, DialogIconEnum.Warning);
            logger.LogError(ex, "Set Machine Absolute Stage Xy failed!");
        }
    }

    [RelayCommand]
    private void LoadBrightImageFilePath()
    {
        try
        {
            var maskDto = GetSelectReticleMaskItem();
            if (dialogWindowProvider.TryShowSelectFilePathDialog(".jpg", out var filePath) == false) return;
            maskDto.RecipeBrightFieldTemplateDto.TemplateImageFilePath = filePath;
            maskDto.RecipeBrightFieldTemplateDto.TemplateFilePath = FileHelper.GetFileFullName(filePath);
        }
        catch (Exception ex)
        {
            dialogWindowProvider.ShowDialog("Load Template Image File Path Failed!",
                DialogButtonsEnum.OK, DialogIconEnum.Warning);
            logger.LogError(ex, "Load Template Image File Path Failed!");
        }
    }

    [RelayCommand]
    private void LoadDarkImageFilePath()
    {
        try
        {
            var maskDto = GetSelectReticleMaskItem();
            if (dialogWindowProvider.TryShowSelectFilePathDialog(".jpg", out var filePath) == false) return;
            maskDto.RecipeDarkFieldTemplateDto.TemplateImageFilePath = filePath;
            maskDto.RecipeDarkFieldTemplateDto.TemplateFilePath = FileHelper.GetFileFullName(filePath);
        }
        catch (Exception ex)
        {
            dialogWindowProvider.ShowDialog("Load Template Image File Path Failed!",
                DialogButtonsEnum.OK, DialogIconEnum.Warning);
            logger.LogError(ex, "Load Template Image File Path Failed!");
        }
    }

    [RelayCommand]
    private async Task GenerateBrightTemplateAsync()
    {
        try
        {
            if (SelectReticleMarkItem is null)
            {
                dialogWindowProvider.ShowDialog("Select Reticle Item Is Empty!",
                    DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return;
            }

            var maskDto = GetSelectReticleMaskItem();
            maskDto.RecipeBrightFieldTemplateDto.TemplateFilePath =
                $@"{TemplateFileDirectory}\{EditRecipeTypeName}\BrightField\Ncc\{maskDto.Remark}_{maskDto.ReticleMaskTypeEnum}_{maskDto.RecipeBrightFieldTemplateDto.MicroscopeLensInformation.LensName}_{Guid.NewGuid()}";

            var templateFilePath = maskDto.RecipeBrightFieldTemplateDto.TemplateFilePath;
            microscopeViewModel.SwitchMicroscopeLensInformation(maskDto.RecipeBrightFieldTemplateDto.MicroscopeLensInformation);
            await Task.Delay(3000);

            if (!reviewViewModel.TryGenerateTemplate(AlgorithmTemplateTypeEnum.Ncc, templateFilePath, AlgorithmTemplateSizeEnum.Size256))
            {
                dialogWindowProvider.ShowDialog("Generate Template Failed",
                    DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return;
            }

            maskDto.RecipeBrightFieldTemplateDto.TemplateImageFilePath =
                CalibrationConstantsHelper.TemplatePathToTemplateImagePath(templateFilePath);
        }
        catch (Exception ex)
        {
            dialogWindowProvider.ShowDialog("Generate Bright Field Template Failed!",
                DialogButtonsEnum.OK, DialogIconEnum.Warning);
            logger.LogError(ex, "Generate Bright Field Template Failed!");
        }
    }

    [RelayCommand]
    private async Task GenerateDarkTemplateAsync()
    {
        try
        {
            var maskDto = GetSelectReticleMaskItem();

            var brightPosition = stageViewModel.GetBrightFieldStagePosition();

            var darkFieldImageDto = await cibViewModel.GetPMTImageAsync(
                maskDto.RecipeDarkFieldTemplateDto.ProductivityInformation,
                StageCoordinateSystemEnum.Bright,
                brightPosition,
                2048,
                maskDto.RecipeDarkFieldTemplateDto.CIBInformation,
                (false, CalChipSiteModelEnum.ChuckModel),
                (false, maskDto.RecipeDarkFieldTemplateDto.OpticsConfiguration),
                (false, maskDto.RecipeDarkFieldTemplateDto.CIBConfiguration),
                (false, maskDto.RecipeDarkFieldTemplateDto.LaserLightInformation),
                false,
                CancellationToken.None);

            using var _ = darkFieldImageDto;

            var templateFilePath = $@"{TemplateFileDirectory}\{EditRecipeTypeName}\DarkField\Ncc\{maskDto.Remark}_{maskDto.ReticleMaskTypeEnum}_{maskDto.RecipeDarkFieldTemplateDto.ProductivityInformation}_{Guid.NewGuid()}";
            var templateImageFilePath = CalibrationConstantsHelper.TemplatePathToTemplateImagePath(templateFilePath);
            darkFieldImageDto.Image.Save(templateImageFilePath);

            createDarkImageTemplateWindowViewModel.ImageFilePath = templateImageFilePath;
            createDarkImageTemplateWindowViewModel.TemplateFilePath = templateFilePath;

            if (windowManagerService.ShowDialog(createDarkImageTemplateWindowViewModel) == false)
            {
                dialogWindowProvider.ShowDialog("Generate Template Failed",
                    DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return;
            }

            maskDto.RecipeDarkFieldTemplateDto.AlgorithmTemplateTypeEnum = createDarkImageTemplateWindowViewModel.AlgorithmTemplateTypeEnum;
            maskDto.RecipeDarkFieldTemplateDto.AlgorithmTemplateSizeEnum = createDarkImageTemplateWindowViewModel.AlgorithmTemplateSizeEnum;
            maskDto.RecipeDarkFieldTemplateDto.TemplateFilePath = createDarkImageTemplateWindowViewModel.TemplateFilePath;
            maskDto.RecipeDarkFieldTemplateDto.TemplateImageFilePath = createDarkImageTemplateWindowViewModel.TemplateImageFilePath;
        }
        catch (Exception ex)
        {
            dialogWindowProvider.ShowDialog("Generate Dark Field Template Failed!",
                DialogButtonsEnum.OK, DialogIconEnum.Warning);
            logger.LogError(ex, "Generate Dark Field Template Failed!");
        }
    }

    private static ObservableCollection<ReticleMarkItemDto> GetReticleMaskConfigList(string? name, CalibrationRecipeDTO dto)
    {
        return name switch
        {
            nameof(ReticleMarkDto.MicrosocpeReticleMarkItemList) => dto.ReticleMarkDto.MicrosocpeReticleMarkItemList,
            nameof(ReticleMarkDto.ChuckReticleMarkItemList) => dto.ReticleMarkDto.ChuckReticleMarkItemList,
            nameof(ReticleMarkDto.LaserReticleMarkItemList) => dto.ReticleMarkDto.LaserReticleMarkItemList,
            _ => throw new ArgumentException("Invalid list name", nameof(name))
        };
    }

    private ReticleMarkItemDto GetSelectReticleMaskItem()
    {
        if (SelectReticleMarkItem is null || EditingDto is null)
        {
            dialogWindowProvider.ShowDialog("Select Reticle Item Is Empty!",
                DialogButtonsEnum.OK, DialogIconEnum.Warning);
            throw new NullReferenceException(nameof(SelectReticleMarkItem));
        }

        var indexed = EditReticleMarkList.Index().FirstOrDefault(t => t.Item.MaskIndex == SelectReticleMarkItem.MaskIndex);
        return EditReticleMarkList[indexed.Index] ?? throw new NullReferenceException(nameof(SelectReticleMarkItem));
    }
}
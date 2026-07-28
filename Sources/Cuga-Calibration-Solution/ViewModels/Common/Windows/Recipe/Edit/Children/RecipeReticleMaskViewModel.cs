using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Algorithm;
using Core.Models.Enums.Stage;
using Core.Models.Helper;
using Core.Models.Models.Common.Cookies;
using Core.Recipe.Models;
using Core.Recipe.Models.Wafer.ReticleMask;
using CugaCalibration.Core.Services.Interfaces;
using CugaCalibration.ViewModels.Common.Windows.Tools;
using CugaCalibration.ViewModels.Common.Windows.Tools.Alignment;
using Local.SQL.Cache.Providers.Services.Interfaces;
using Microsoft.Extensions.Logging;
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
using Net.Utilities.Calibration;

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
    ICalibrationRecipeService calibrationRecipeService,
    MicroscopeViewModel microscopeViewModel,
    ReviewViewModel reviewViewModel,
    CIBViewModel cibViewModel,
    StageViewModel stageViewModel,
    RecipeCookie recipeCookie,
    ApplicationCookie applicationCookie,
    CreateDarkImageTemplateWindowViewModel createDarkImageTemplateWindowViewModel,
    AlignmentWindowDarkFieldViewModel alignmentWindowDarkFieldViewModel) : ViewModelBase
{
    public ApplicationCookie ApplicationCookie { get; } = applicationCookie;
    public AlignmentWindowDarkFieldViewModel AlignmentWindowDarkFieldViewModel { get; } = alignmentWindowDarkFieldViewModel;

    [ObservableProperty]
    public partial RecipeReticleMarkViewUserControlViewModel RecipeReticleMarkViewUserControlViewModel { get; set; } = new();

    [ObservableProperty]
    private ReticleMarkDTOItem? _selectReticleMarkItem;

    /// <summary>
    /// 当前 Tab 对应的列表（编辑用）
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<ReticleMarkDTOItem> _editReticleMarkList = [];

    // 内部引用（由主 VM 注入）

    /// <summary>
    /// 当前编辑中的配方
    /// </summary>
    public CalibrationRecipeDTO? EditingDTO { get; set; }

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
    public RecipeWaferMapViewModel? RecipeWaferMapViewModel { get; set; }

    public void NotifyAll()
    {
        contextProvider.Post(() => { OnPropertyChanged(nameof(EditingDTO)); });
    }

    [RelayCommand]
    public void ParamTypeChanged(object obj)
    {
        if (obj is MouseButtonEventArgs e)
            e.Handled = true;

        // if (EditRecipeTypeName.Contains("Wafer")) RecipeWaferMapViewModel?.NotifyWaferMapView();
    }

    [RelayCommand]
    public void MaskTypeChanged(object obj) => RefreshReticleView(obj);

    [RelayCommand]
    public void Loading(object obj) => RefreshReticleView(obj);

    private void RefreshReticleView(object obj)
    {
        if (EditingDTO is null) return;
        RecipeReticleMarkViewUserControlViewModel.WaferDTO = EditingDTO.WaferDTO;

        var header = obj switch
        {
            TabItem tabItem => tabItem.Header?.ToString(),
            string s => s,
            _ => null
        };

        if (header is null) return;

        var listName = header switch
        {
            "Microscope" => nameof(ReticleMarkDTO.MicroscopeReticleMarks),
            "Chuck" => nameof(ReticleMarkDTO.ChuckReticleMarks),
            "Laser" => nameof(ReticleMarkDTO.LaserReticleMarks),
            _ => null
        };

        if (listName is null) return;

        EditReticleMarkList = GetReticleMaskConfigList(listName, EditingDTO);
        EditRecipeTypeName = header;
        RefreshReticleMaskView();
    }

    private void RefreshReticleMaskView() => RecipeReticleMarkViewUserControlViewModel.ReticleMarkList = [.. EditReticleMarkList];

    [RelayCommand]
    private void AddMask()
    {
        if (EditingDTO is null) return;
        var list = EditReticleMarkList;
        contextProvider.Send(() => list.Add(new ReticleMarkDTOItem
        {
            MaskIndex = list.Count != 0 ? list.Last().MaskIndex + 1 : 0
        }));
    }

    [RelayCommand]
    private void DeleteMask()
    {
        if (SelectReticleMarkItem is null || EditingDTO is null) return;
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
            if (RecipeWaferMapViewModel is null) return;

            Guard.IsNotNull(EditingDTO);

            var maskDto = GetSelectReticleMaskItem();

            // 使用P8结果Build Wafer，Wafer坐标等价于明场坐标，可以和机械坐标互相转换
            var maskMachinePosition = stageViewModel.GetMachineStagePosition();

            var waferMapDie = calibrationRecipeService.GetCurrentWaferMapDie(EditingDTO.WaferDTO, maskMachinePosition);
            var waferMapReticle = calibrationRecipeService.GetCurrentWaferMapReticle(EditingDTO.WaferDTO, maskMachinePosition);
            var maskWaferPosition = stageViewModel.GetBrightFieldStagePosition();
            // todo：reticle

            maskDto.MaskWaferCellPosition = maskWaferPosition - (Vector)waferMapDie.Rect.Point;
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
            if (RecipeWaferMapViewModel is null) return;

            Guard.IsNotNull(EditingDTO);

            var maskDto = GetSelectReticleMaskItem();
            microscopeViewModel.SwitchMicroscopeLensInformation(maskDto.RecipeBrightFieldTemplateDTO.MicroscopeLensInformation);

            // todo：reticle
            var machinePosition = stageViewModel.GetMachineStagePosition();
            var waferMapDie = calibrationRecipeService.GetCurrentWaferMapDie(EditingDTO.WaferDTO, machinePosition);
            calibrationRecipeService.GetMaskMachinePosition(
                EditingDTO.WaferDTO,
                waferMapDie,
                maskDto,
                false,
                out var maskMachinePosition);

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
            maskDto.RecipeBrightFieldTemplateDTO.TemplateImageFilePath = filePath;
            maskDto.RecipeBrightFieldTemplateDTO.TemplateFilePath = FileHelper.GetFileFullName(filePath);
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
            maskDto.RecipeDarkFieldTemplateDTO.TemplateImageFilePath = filePath;
            maskDto.RecipeDarkFieldTemplateDTO.TemplateFilePath = FileHelper.GetFileFullName(filePath);
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
            maskDto.RecipeBrightFieldTemplateDTO.TemplateFilePath =
                $@"{TemplateFileDirectory}\{EditRecipeTypeName}\BrightField\Ncc\{maskDto.Remark}_{maskDto.ReticleMaskTypeEnum}_{maskDto.RecipeBrightFieldTemplateDTO.MicroscopeLensInformation.LensName}_{Guid.NewGuid()}";

            var templateFilePath = maskDto.RecipeBrightFieldTemplateDTO.TemplateFilePath;
            microscopeViewModel.SwitchMicroscopeLensInformation(maskDto.RecipeBrightFieldTemplateDTO.MicroscopeLensInformation);
            await Task.Delay(3000);

            if (!reviewViewModel.TryGenerateTemplate(AlgorithmTemplateTypeEnum.Ncc, templateFilePath, AlgorithmTemplateSizeEnum.Size256))
            {
                dialogWindowProvider.ShowDialog("Generate Template Failed",
                    DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return;
            }

            maskDto.RecipeBrightFieldTemplateDTO.TemplateImageFilePath =
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
                maskDto.RecipeDarkFieldTemplateDTO.ProductivityInformation,
                StageCoordinateSystemEnum.Bright,
                brightPosition,
                2048,
                maskDto.RecipeDarkFieldTemplateDTO.CIBInformation,
                (false, CalChipSiteModelEnum.ChuckModel),
                (false, maskDto.RecipeDarkFieldTemplateDTO.OpticsConfiguration),
                (false, maskDto.RecipeDarkFieldTemplateDTO.CIBConfiguration),
                (false, maskDto.RecipeDarkFieldTemplateDTO.LaserLightInformation),
                false,
                CancellationToken.None);

            using var _ = darkFieldImageDto;

            var templateFilePath = $@"{TemplateFileDirectory}\{EditRecipeTypeName}\DarkField\Ncc\{maskDto.Remark}_{maskDto.ReticleMaskTypeEnum}_{maskDto.RecipeDarkFieldTemplateDTO.ProductivityInformation}_{Guid.NewGuid()}";
            var templateImageFilePath = CalibrationConstantsHelper.TemplatePathToTemplateImagePath(templateFilePath);
            darkFieldImageDto.Image.SaveImage(templateImageFilePath);

            createDarkImageTemplateWindowViewModel.ImageFilePath = templateImageFilePath;
            createDarkImageTemplateWindowViewModel.TemplateFilePath = templateFilePath;

            if (windowManagerService.ShowDialog(createDarkImageTemplateWindowViewModel) == false)
            {
                dialogWindowProvider.ShowDialog("Generate Template Failed",
                    DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return;
            }

            maskDto.RecipeDarkFieldTemplateDTO.AlgorithmTemplateTypeEnum = createDarkImageTemplateWindowViewModel.AlgorithmTemplateTypeEnum;
            maskDto.RecipeDarkFieldTemplateDTO.AlgorithmTemplateSizeEnum = createDarkImageTemplateWindowViewModel.AlgorithmTemplateSizeEnum;
            maskDto.RecipeDarkFieldTemplateDTO.TemplateFilePath = createDarkImageTemplateWindowViewModel.TemplateFilePath;
            maskDto.RecipeDarkFieldTemplateDTO.TemplateImageFilePath = createDarkImageTemplateWindowViewModel.TemplateImageFilePath;
        }
        catch (Exception ex)
        {
            dialogWindowProvider.ShowDialog("Generate Dark Field Template Failed!",
                DialogButtonsEnum.OK, DialogIconEnum.Warning);
            logger.LogError(ex, "Generate Dark Field Template Failed!");
        }
    }

    private static ObservableCollection<ReticleMarkDTOItem> GetReticleMaskConfigList(string? name, CalibrationRecipeDTO dto)
    {
        return name switch
        {
            nameof(ReticleMarkDTO.MicroscopeReticleMarks) => dto.ReticleMarkDTO.MicroscopeReticleMarks,
            nameof(ReticleMarkDTO.ChuckReticleMarks) => dto.ReticleMarkDTO.ChuckReticleMarks,
            nameof(ReticleMarkDTO.LaserReticleMarks) => dto.ReticleMarkDTO.LaserReticleMarks,
            _ => throw new ArgumentException("Invalid list name", nameof(name))
        };
    }

    private ReticleMarkDTOItem GetSelectReticleMaskItem()
    {
        if (SelectReticleMarkItem is null || EditingDTO is null)
        {
            dialogWindowProvider.ShowDialog("Select Reticle Item Is Empty!",
                DialogButtonsEnum.OK, DialogIconEnum.Warning);
            throw new NullReferenceException(nameof(SelectReticleMarkItem));
        }

        var indexed = EditReticleMarkList.Index().FirstOrDefault(t => t.Item.MaskIndex == SelectReticleMarkItem.MaskIndex);
        return EditReticleMarkList[indexed.Index] ?? throw new NullReferenceException(nameof(SelectReticleMarkItem));
    }
}
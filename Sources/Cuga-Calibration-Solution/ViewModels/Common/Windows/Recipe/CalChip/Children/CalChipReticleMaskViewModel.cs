using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Algorithm;
using Core.Models.Enums.Stage;
using Core.Models.Helper;
using Core.Models.Models.Common.Cookies;
using Core.Recipe.Models;
using Core.Recipe.Models.Wafer.ReticleMask;
using Core.Utilities;
using CugaCalibration.Core.Services.Interfaces;
using CugaCalibration.ViewModels.Common.Windows.Recipe.Edit.Children;
using CugaCalibration.ViewModels.Common.Windows.Tools;
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

namespace CugaCalibration.ViewModels.Common.Windows.Recipe.CalChip.Children;

[IOCAppService(ServiceType = typeof(CalChipReticleMaskViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class CalChipReticleMaskViewModel(
    ILogger<CalChipReticleMaskViewModel> logger,
    IDialogWindowProvider dialogWindowProvider,
    IWindowManagerService windowManagerService,
    ISynchronizationContextProvider contextProvider,
    ICalibrationRecipeService calibrationRecipeService,
    MicroscopeViewModel microscopeViewModel,
    ReviewViewModel reviewViewModel,
    CIBViewModel cibViewModel,
    StageViewModel stageViewModel,
    ApplicationCookie applicationCookie,
    CreateDarkImageTemplateWindowViewModel createDarkImageTemplateWindowViewModel) : ViewModelBase
{
    public ApplicationCookie ApplicationCookie { get; } = applicationCookie;

    [ObservableProperty]
    public partial RecipeReticleMarkViewUserControlViewModel RecipeReticleMarkViewUserControlViewModel { get; set; } = new();

    [ObservableProperty]
    public partial ReticleMarkDTOItem? SelectReticleMarkItem { get; set; }

    [ObservableProperty]
    private ObservableCollection<ReticleMarkDTOItem> _editReticleMarkList = [];

    private CalChipRecipeDTO? _editDTO;

    /// <summary>
    /// 当前编辑中的 CalChip 配方项
    /// </summary>
    public CalChipRecipeDTOItem? EditingItem { get; private set; }

    [ObservableProperty]
    public partial CalChipSiteModelEnum CurrentCalChipSiteModelEnum { get; set; }

    public string TemplateFileDirectory { get; set; } = string.Empty;

    public CalChipWaferMapViewModel? CalChipWaferMapViewModel { get; set; }

    public void Initialize(CalChipRecipeDTO calChipRecipeDTO)
    {
        _editDTO = calChipRecipeDTO;
        EditingItem = _editDTO.CurrentItem;
        CurrentCalChipSiteModelEnum = _editDTO.CalChipSiteModelEnum;

        RecipeReticleMarkViewUserControlViewModel.WaferDTO = EditingItem.CalChipMapDTO;
        EditReticleMarkList = EditingItem.ReticleMarks;
        RefreshReticleMaskView();
    }

    public void NotifyAll()
    {
        contextProvider.Post(() =>
        {
            OnPropertyChanged(nameof(EditingItem));
            OnPropertyChanged(nameof(EditReticleMarkList));
            OnPropertyChanged(nameof(RecipeReticleMarkViewUserControlViewModel));
        });
    }

    private void RefreshReticleMaskView() => RecipeReticleMarkViewUserControlViewModel.ReticleMarkList = [.. EditReticleMarkList];

    [RelayCommand]
    private void AddMask()
    {
        if (EditingItem is null) return;
        var list = EditReticleMarkList;
        contextProvider.Send(() => list.Add(new ReticleMarkDTOItem
        {
            MaskIndex = list.Count != 0 ? list.Last().MaskIndex + 1 : 0
        }));
    }

    [RelayCommand]
    private void DeleteMask()
    {
        if (SelectReticleMarkItem is null || EditingItem is null) return;
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
            if (CalChipWaferMapViewModel is null) return;

            Guard.IsNotNull(EditingItem);

            var maskDto = GetSelectReticleMaskItem();

            var maskMachinePosition = stageViewModel.GetMachineStagePosition();

            var waferMapDie = calibrationRecipeService.GetCurrentWaferMapDie(EditingItem.CalChipMapDTO, maskMachinePosition);
            var maskWaferPosition = stageViewModel.GetBrightFieldStagePosition();

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
            if (CalChipWaferMapViewModel is null) return;

            Guard.IsNotNull(EditingItem);

            var maskDto = GetSelectReticleMaskItem();
            microscopeViewModel.SwitchMicroscopeLensInformation(maskDto.RecipeBrightFieldTemplateDTO.MicroscopeLensInformation);

            var machinePosition = stageViewModel.GetMachineStagePosition();
            var waferMapDie = calibrationRecipeService.GetCurrentWaferMapDie(EditingItem.CalChipMapDTO, machinePosition);
            calibrationRecipeService.GetMaskMachinePosition(
                EditingItem.CalChipMapDTO,
                waferMapDie,
                maskDto,
                false,
                out var maskMachinePosition);

            stageViewModel.SetCalChipBrightFieldAbsoluteStageXy(stageViewModel.MachineToBrightFieldPosition(maskMachinePosition), CurrentCalChipSiteModelEnum);
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
                $@"{TemplateFileDirectory}\BrightField\Ncc\{maskDto.Remark}_{maskDto.ReticleMaskTypeEnum}_{maskDto.RecipeBrightFieldTemplateDTO.MicroscopeLensInformation.LensName}_{Guid.NewGuid()}";

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
                (false, CurrentCalChipSiteModelEnum),
                (false, maskDto.RecipeDarkFieldTemplateDTO.OpticsConfiguration),
                (false, maskDto.RecipeDarkFieldTemplateDTO.CIBConfiguration),
                (false, maskDto.RecipeDarkFieldTemplateDTO.LaserLightInformation),
                false,
                CancellationToken.None);

            using var _ = darkFieldImageDto;

            var templateFilePath = $@"{TemplateFileDirectory}\DarkField\Ncc\{maskDto.Remark}_{maskDto.ReticleMaskTypeEnum}_{maskDto.RecipeDarkFieldTemplateDTO.ProductivityInformation}_{Guid.NewGuid()}";
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

    private ReticleMarkDTOItem GetSelectReticleMaskItem()
    {
        if (SelectReticleMarkItem is null || EditingItem is null)
        {
            dialogWindowProvider.ShowDialog("Select Reticle Item Is Empty!",
                DialogButtonsEnum.OK, DialogIconEnum.Warning);
            throw new NullReferenceException(nameof(SelectReticleMarkItem));
        }

        var indexed = EditReticleMarkList.Index().FirstOrDefault(t => t.Item.MaskIndex == SelectReticleMarkItem.MaskIndex);
        return EditReticleMarkList[indexed.Index] ?? throw new NullReferenceException(nameof(SelectReticleMarkItem));
    }
}
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Core.Models.Helper;
using CugaCalibration.ViewModels.Common.Windows.File.Save;
using CugaCalibration.ViewModels.Common.Windows.Management.Recipe.Management;
using CugaCalibration.ViewModels.Common.Windows.Tools;
using CugaCalibration.ViewModels.Common.Windows.Tools.Alignment;
using Local.SQL.DB.Providers.Models.Entities.DTO;
using Microsoft.Extensions.Logging;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM;
using Net.Utilities.WPF.MVVM.Events;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;

namespace CugaCalibration.ViewModels;

public sealed partial class MainWindowViewModel : IRecipient<PopupWindowEvent>
{
    [ObservableProperty]
    public partial bool IsMenuEnable { get; set; } = true;

    [RelayCommand]
    private async Task OpenToolMenuAsync(SysMenuDTO sysMenu)
    {
        await Task.Run(async () =>
        {
            try
            {
                var viewModel = sysMenu.Component;
                if (string.IsNullOrWhiteSpace(viewModel)) return;

                var viewModelBase = HostApplication.GetRequiredService<ViewModelBase>(viewModel);
                if (viewModelBase is null) return;
                switch (viewModelBase)
                {
                    case MainWindowViewModel:
                        switch (sysMenu.Name)
                        {
                            case CalibrationConstantsHelper.Export:
                                if (_dialogWindowProvider.TryShowSaveFilePathDialog(".json", out var exportPath) == true)
                                {
                                    var (isSuccess, message) = await _calibrationCacheProviderService.TryExportAsync(exportPath, CancellationToken.None);
                                    if (isSuccess)
                                        _dialogWindowProvider.ShowDialog(message);
                                    else
                                        _dialogWindowProvider.ShowDialog(message, DialogButtonsEnum.OK, DialogIconEnum.Error);
                                }

                                break;

                            case CalibrationConstantsHelper.Import:
                                if (_dialogWindowProvider.TryShowSelectFilePathDialog(".json", out var importPath) == true)
                                {
                                    var (isSuccess, message) = await _calibrationCacheProviderService.TryImportAsync(importPath, CancellationToken.None);

                                    if (isSuccess)
                                        _dialogWindowProvider.ShowDialog(message);
                                    else
                                        _dialogWindowProvider.ShowDialog(message, DialogButtonsEnum.OK, DialogIconEnum.Error);
                                }

                                break;
                        }

                        break;
                    case SaveFileWindowViewModel saveFileWindowViewModel:
                        _windowManagerService.ShowDialog(saveFileWindowViewModel);

                        break;

                    case PopupWindowViewModelBase popupWindowViewModel:
                        if (popupWindowViewModel.Show() == false) _windowManagerService.ShowWindow(popupWindowViewModel);

                        break;

                    case AlignmentWindowBrightFieldViewModel alignmentWindowViewModel:
                        alignmentWindowViewModel.IsShowAlign = true;
                        _windowManagerService.ShowDialog(alignmentWindowViewModel);
                        break;

                    case CreateDarkImageTemplateWindowViewModel createDarkImageTemplateWindowViewModel:
                        var dialog = _dialogWindowProvider.TryShowSelectFilePathDialog(".jpg", out var filePath);
                        if (dialog == false) return;

                        createDarkImageTemplateWindowViewModel.ImageFilePath = filePath;
                        createDarkImageTemplateWindowViewModel.TemplateFilePath = $"{FileHelper.GetFileFullName(filePath)}_Template";
                        _windowManagerService.ShowDialog(createDarkImageTemplateWindowViewModel);

                        break;

                    case ApplicationAboutWindowViewModel applicationAboutWindowViewModel:
                        _windowManagerService.ShowDialog(applicationAboutWindowViewModel);
                        break;

                    case RecipeManagementViewModel recipeManagementViewModel:
                        recipeManagementViewModel.IsLoading = false;
                        _windowManagerService.ShowDialog(recipeManagementViewModel);
                        break;

                    default:
                        _windowManagerService.ShowDialog(viewModelBase);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "{@Name}: Load Menu View({@ViewModel}) Failed", nameof(MainWindowViewModel), sysMenu.Name);
            }
        });
    }

    public void Receive(PopupWindowEvent message)
    {
        IsMenuEnable = message.IsPopupWindowEnable;
    }
}
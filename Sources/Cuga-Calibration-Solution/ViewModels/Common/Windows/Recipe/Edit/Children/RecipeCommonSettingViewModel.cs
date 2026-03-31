using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Core.Models.Events;
using Core.Models.Helper;
using Core.Recipe.Models;
using Local.SQL.Cache.Providers.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;

namespace CugaCalibration.ViewModels.Common.Windows.Recipe.Edit.Children;

[IOCAppService(ServiceType = typeof(RecipeCommonSettingViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class RecipeCommonSettingViewModel(
    [FromKeyedServices(CalibrationConstantsHelper.RecipeDbKey)]
    ICacheProvider recipeCacheProvider,
    IDialogWindowProvider dialogWindowProvider,
    IMessenger messenger,
    RecipeCookie recipeCookie) : ViewModelBase
{
    /// <summary>
    /// 当前编辑中的配方副本（由主 VM 在 Loaded 时赋值）
    /// </summary>
    [ObservableProperty]
    private CalibrationRecipeDTO _calibrationRecipeDto = new();

    public void Initialize(CalibrationRecipeDTO dto)
    {
        CalibrationRecipeDto = dto;
    }

    [RelayCommand]
    public async Task SaveAsync(Action closeViewAction)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        try
        {
            CalibrationRecipeDto.WaferDto.WaferMapCanvasDocumentToWaferMapData();
            recipeCacheProvider.Set(CalibrationRecipeDto, CancellationToken.None);

            dialogWindowProvider.TryShowDialog("Do you want to apply this recipe? ",
                out DialogResultEnum dialogResultEnum,
               DialogButtonsEnum.YesNo,
               DialogIconEnum.Question);

            if (dialogResultEnum == DialogResultEnum.Yes)
            {
                recipeCookie.CalibrationRecipeDto.AdaptIn(CalibrationRecipeDto);
                recipeCookie.CalibrationReviseRecipeDto.AdaptIn(CalibrationRecipeDto);
                messenger.Send(ToggleCalibrateEventFactory.RefreshWindow(true));
                messenger.Send(ToggleRecipeEventFactory.RefreshRecipeManagementView(true));
            }

            closeViewAction();
        }
        catch (Exception ex)
        {
            dialogWindowProvider.ShowDialog($"Save Recipe failed! {ex.Message}",
               DialogButtonsEnum.OK,
               DialogIconEnum.Warning);
        }
    }
}

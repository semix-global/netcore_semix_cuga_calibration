using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Recipe.Services.Interfaces.Factory;
using Local.SQL.DB.Providers.Models.Entities.DTO;
using Local.SQL.DB.Providers.Services.Interfaces;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;

namespace CugaCalibration.ViewModels.Common.Windows.Management.Recipe.Management;

[IOCAppService(ServiceType = typeof(RecipeInformationEditViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public partial class RecipeInformationEditViewModel(
    IDialogWindowProvider dialogWindowProvider,
    ISysRecipeInformationService sysRecipeInformationService,
    ISysRecipeInformationDtoFactory sysRecipeInformationDtoFactory
) : ViewModelBase
{
    [ObservableProperty]
    private SysRecipeInformationDTO? _currentSysRecipeInformation;

    [ObservableProperty]
    private SysRecipeInformationDTO? _editSysRecipeInformation;

    public async Task LoadAsync(
        SysRecipeInformationDTO sysRecipeInformationDto)
    {
        await Task.CompletedTask.ConfigureAwait(false);

        CurrentSysRecipeInformation = sysRecipeInformationDto;

        EditSysRecipeInformation = sysRecipeInformationDto.Clone();
    }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.CompletedTask.ConfigureAwait(false);

            Guard.IsNotNull(EditSysRecipeInformation);

            var updateDto = sysRecipeInformationDtoFactory.CreateFrom(EditSysRecipeInformation, EditSysRecipeInformation.RecipeDbName);

            await sysRecipeInformationService
                .UpdateAsync(updateDto, cancellationToken)
                .ConfigureAwait(false);

            CurrentSysRecipeInformation = updateDto;

            await CancelAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            dialogWindowProvider.ShowDialog($"Update recipe failed! {ex.Message}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
        }
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        await Task.CompletedTask.ConfigureAwait(false);

        EditSysRecipeInformation = null;

        CloseView(null);
    }
}
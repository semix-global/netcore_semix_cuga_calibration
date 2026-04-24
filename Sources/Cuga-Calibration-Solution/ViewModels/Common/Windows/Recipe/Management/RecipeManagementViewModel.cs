using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Core.Models.Events;
using Core.Models.Helper;
using Core.Recipe.Models;
using Core.Recipe.Services.Interfaces.Factory;
using Core.Utilities;
using Local.SQL.Cache.Providers.Extensions;
using Local.SQL.Cache.Providers.Interfaces;
using Local.SQL.DB.Providers.Models.Entities.DTO;
using Local.SQL.DB.Providers.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.IOC.Providers;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.Services;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace CugaCalibration.ViewModels.Common.Windows.Management.Recipe.Management;

[IOCAppService(ServiceType = typeof(RecipeManagementViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public partial class RecipeManagementViewModel : ViewModelBase, IRecipient<ValueChangedMessage<ToggleRecipeEvent>>
{
    private CancellationTokenSource? _cancellationTokenSource;

    #region 属性

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private RecipeCookie _recipeCookie;

    [ObservableProperty]
    private SysRecipeInformationDTO? _selectRecipeInfoDto;

    [ObservableProperty]
    private ObservableCollection<SysRecipeInformationDTO> _recipeInfoDtoItems = [];

    private readonly IDialogWindowProvider _dialogWindowProvider;
    private readonly IWindowManagerService _windowManagerService;
    private readonly IMessenger _messenger;
    private readonly ICacheProvider _cacheProvider;
    private readonly ICacheDatabaseProvider _cacheDatabaseProvider;
    private readonly ISysRecipeInformationService _sysRecipeInformationService;
    private readonly IOptions<ApplicationSetting> _options;
    private readonly ISynchronizationContextProvider _contextProvider;
    private readonly ISysRecipeInformationDtoFactory _sysRecipeInformationDtoFactory;
    private readonly RecipeInformationEditViewModel _recipeInformationEditViewModel;

    /// <inheritdoc/>
    public RecipeManagementViewModel(
        IDialogWindowProvider dialogWindowProvider,
        IWindowManagerService windowManagerService,
        IMessenger messenger,
        [FromKeyedServices(CalibrationConstantsHelper.RecipeDbKey)]
        ICacheProvider cacheProvider,
        [FromKeyedServices(CalibrationConstantsHelper.RecipeDbKey)]
        ICacheDatabaseProvider cacheDatabaseProvider,
        ISysRecipeInformationService sysRecipeInformationService,
        IOptions<ApplicationSetting> options,
        ISynchronizationContextProvider contextProvider,
        ISysRecipeInformationDtoFactory sysRecipeInformationDtoFactory,
        RecipeInformationEditViewModel recipeInformationEditViewModel,
        RecipeCookie recipeCookie)
    {
        _dialogWindowProvider = dialogWindowProvider;
        _windowManagerService = windowManagerService;
        _messenger = messenger;
        _cacheProvider = cacheProvider;
        _cacheDatabaseProvider = cacheDatabaseProvider;
        _sysRecipeInformationService = sysRecipeInformationService;
        _options = options;
        _contextProvider = contextProvider;
        _sysRecipeInformationDtoFactory = sysRecipeInformationDtoFactory;
        _recipeInformationEditViewModel = recipeInformationEditViewModel;
        _recipeCookie = recipeCookie;

        messenger.RegisterAll(this);
    }

    #endregion


    [RelayCommand]
    private async Task LoadedAsync()
    {
        try
        {
            RefreshToken();

            RecipeInfoDtoItems = [];
            var resultList = await _sysRecipeInformationService
                .GetAllAsync()
                .ConfigureAwait(false);

            if (resultList.Count == 0) await InsertAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _dialogWindowProvider.ShowDialog($"Load recipe list failed! {ex.Message}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
        }
        finally
        {
            await RefreshRecipeListAsync().ConfigureAwait(false);
        }
    }

    [RelayCommand]
    private async Task InsertAsync()
    {
        await Task.Run(async () =>
        {
            try
            {
                var defaultSysRecipeInformationDto = _sysRecipeInformationDtoFactory.Create(_options.Value.DefaultRecipeName + Guid.NewGuid());

                SelectRecipeInfoDto = await _sysRecipeInformationService
                    .CreatAsync(defaultSysRecipeInformationDto, _cancellationTokenSource.Token)
                    .ConfigureAwait(false);

                await EditRecipeInformationAsync(SelectRecipeInfoDto).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _dialogWindowProvider.ShowDialog($"Insert recipe failed! {ex.Message}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            }
            finally
            {
                await RefreshRecipeListAsync().ConfigureAwait(false);
            }
        });
    }

    [RelayCommand]
    private async Task EditRecipeInformationAsync(SysRecipeInformationDTO selectedItem)
    {
        await Task.Run(async () =>
        {
            try
            {
                Guard.IsNotNull(selectedItem, "Please Select a Recipe!");

                SelectRecipeInfoDto = selectedItem;
                await _recipeInformationEditViewModel
                    .LoadAsync(SelectRecipeInfoDto)
                    .ConfigureAwait(false);

                _windowManagerService.ShowDialog(_recipeInformationEditViewModel);
            }
            catch (Exception ex)
            {
                _dialogWindowProvider.ShowDialog($"Update recipe failed! {ex.Message}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            }
            finally
            {
                await RefreshRecipeListAsync().ConfigureAwait(false);
            }
        });
    }

    [RelayCommand]
    private async Task DeleteAsync(SysRecipeInformationDTO selectedItem)
    {
        try
        {
            Guard.IsNotNull(selectedItem, "Please Select a Recipe!");

            _dialogWindowProvider.TryShowDialog($"Do you need to delete recipe '{selectedItem.RecipeDbName}'?", out var dialogResultEnum, DialogButtonsEnum.YesNo, DialogIconEnum.Question);
            if (dialogResultEnum != DialogResultEnum.Yes)
                return;

            if (string.Equals(RecipeCookie.SysRecipeInformationDTO.RecipeDbName, selectedItem.RecipeDbName, StringComparison.OrdinalIgnoreCase))
                _dialogWindowProvider.ShowDialog("Cannot delete the currently applied recipe. Please switch to another recipe before deleting", DialogButtonsEnum.OK, DialogIconEnum.Warning);

            await _sysRecipeInformationService
                .DeleteAsync(selectedItem, _cancellationTokenSource.Token)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _dialogWindowProvider.ShowDialog($"Delete recipe failed! {ex.Message}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
        }
        finally
        {
            await RefreshRecipeListAsync().ConfigureAwait(false);
        }
    }

    [RelayCommand]
    private async Task ImportAsync()
    {
        await Task.Run(async () =>
        {
            try
            {
                if (_dialogWindowProvider.TryShowSelectDirectoryPathDialog(out var directoryPath) == false)
                    return;

                await _sysRecipeInformationService
                    .ImportAsync(directoryPath, _options.Value.NosqlDbDataSourceDirectory, _cancellationTokenSource.Token)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _dialogWindowProvider.ShowDialog($"Import recipe failed! {ex.Message}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            }
            finally
            {
                await RefreshRecipeListAsync().ConfigureAwait(false);
            }
        });
    }

    [RelayCommand]
    private async Task InheritAsync(SysRecipeInformationDTO selectedItem)
    {
        await Task.Run(async () =>
        {
            try
            {
                Guard.IsNotNull(selectedItem, "Please Select a Recipe!");

                var sourceDirectoryPath = SQLiteHelper.GetDatabaseDirectoryPath(selectedItem.RecipeNosqlRecipeDbDataSource);

                await _sysRecipeInformationService
                    .ImportAsync(sourceDirectoryPath, _options.Value.NosqlDbDataSourceDirectory, _cancellationTokenSource.Token)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _dialogWindowProvider.ShowDialog($"Import recipe failed! {ex.Message}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            }
            finally
            {
                await RefreshRecipeListAsync().ConfigureAwait(false);
            }
        });
    }

    [RelayCommand]
    private async Task ApplyRecipeAsync()
    {
        await Task.Run(() =>
        {
            try
            {
                Guard.IsNotNull(SelectRecipeInfoDto, "Please Select a Recipe!");

                _cacheDatabaseProvider.ChangeDatabase(SelectRecipeInfoDto.RecipeNosqlRecipeDbDataSource, _cancellationTokenSource.Token);

                var (isHas, calibrationRecipeDto) = _cacheProvider.TryGetOrDefault<CalibrationRecipeDTO>();
                if (isHas == false)
                {
                    _dialogWindowProvider.ShowDialog("Failed to read recipe database, will apply default values.", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    _cacheProvider.Set(new CalibrationRecipeDTO(), CancellationToken.None);
                }

                RecipeCookie.CalibrationRecipeDto.AdaptIn(calibrationRecipeDto);
                RecipeCookie.SysRecipeInformationDTO.AdaptIn(SelectRecipeInfoDto);

                Close();

                _messenger.Send(ToggleCalibrateEventFactory.RefreshWindow(true)); // 刷新界面
            }
            catch (Exception ex)
            {
                _dialogWindowProvider.ShowDialog($"Apply recipe failed! {ex.Message}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            }
        });
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

    private async Task RefreshRecipeListAsync()
    {
        var resultList = await _sysRecipeInformationService.GetAllAsync().ConfigureAwait(false);
        RecipeInfoDtoItems = [.. resultList.Where(t => t.IsDeleted == false)];
    }

    public void Receive(ValueChangedMessage<ToggleRecipeEvent> message)
    {
        _contextProvider.Post(async () =>
        {
            if (message.Value.IsRefreshRecipeList.HasValue)
            {
                if (message.Value.IsRefreshRecipeList.Value)
                {
                    await LoadedAsync().ConfigureAwait(false);
                    OnPropertyChanged(nameof(RecipeCookie));
                }
            }
        });
    }

    [RelayCommand]
    private void Close()
    {
        var isAppliedRecipe = RecipeInfoDtoItems.SingleOrDefault(t => t.RecipeDbName == RecipeCookie.SysRecipeInformationDTO.RecipeDbName) is not null;
        if (isAppliedRecipe == false)
        {
            _dialogWindowProvider.TryShowDialog("The recipe is renamed! Please select again!", out _, DialogButtonsEnum.OK, DialogIconEnum.Warning);
            return;
        }

        CloseView(isAppliedRecipe);
    }
}
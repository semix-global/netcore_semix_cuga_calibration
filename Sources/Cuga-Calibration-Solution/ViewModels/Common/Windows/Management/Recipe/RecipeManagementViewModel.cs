using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Core.Models.Events;
using Core.Models.Helper;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Common.Recipe;
using Core.Models.Models.Setting;
using Core.Utilities;
using Local.NoSQL.DB.Providers.Extensions;

using Local.NoSQL.DB.Providers.Interfaces;
using Local.SQL.DB.Providers.Models.Entities.DTO;
using Local.SQL.DB.Providers.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.IOC.Providers;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.Services;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using System.Collections.ObjectModel;
using System.IO;

namespace CugaCalibration.ViewModels.Common.Windows.Management.Recipe;

[IOCAppService(ServiceType = typeof(RecipeManagementViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class RecipeManagementViewModel : ViewModelBase, IRecipient<ValueChangedMessage<ToggleRecipeEvent>>
{
    [ObservableProperty]
    private ApplicationCookie _applicationCookie;

    [ObservableProperty]
    private CalibrationRecipeDto? _selectRecipeDto;

    [ObservableProperty]
    private SysRecipeInformationDto? _selectRecipeInfoDto;

    [ObservableProperty]
    private ObservableCollection<SysRecipeInformationDto>? _recipeInfoDtoItems = [];

    private CalibrationSetting _calibrationSetting;

    private readonly IDialogWindowProvider _dialogWindowProvider;
    private readonly ICacheProvider _recipeCacheProvider;
    private readonly IWindowManagerService _windowManagerService;
    private readonly ISynchronizationContextProvider _contextProvider;
    private readonly IMessenger _messenger;
    private readonly ISysRecipeInformationService _sysRecipeInformationService;
    private readonly ICacheDatabaseProvider _cacheDatabaseProvider;
    private readonly IOptions<ApplicationSetting> _options;
    private readonly RecipeSettingViewModel _recipeSettingViewModel;

    public RecipeManagementViewModel(
        IDialogWindowProvider dialogWindowProvider,
        IWindowManagerService windowManagerService,
        ISynchronizationContextProvider contextProvider,
        IMessenger messenger,
        ISysRecipeInformationService sysRecipeInformationService,
        [FromKeyedServices(CalibrationConstantsHelper.RecipeDbKey)]
        ICacheDatabaseProvider cacheDatabaseProvider,
        IOptions<ApplicationSetting> options,
        RecipeSettingViewModel recipeSettingViewModel,
        ApplicationCookie applicationCookie,
        CalibrationSetting calibrationSetting)
    {
        _dialogWindowProvider = dialogWindowProvider;
        _recipeCacheProvider = HostApplication.GetKeyedService<ICacheProvider>(CalibrationConstantsHelper.RecipeDbKey)!;
        _windowManagerService = windowManagerService;
        _contextProvider = contextProvider;
        _messenger = messenger;
        _sysRecipeInformationService = sysRecipeInformationService;
        _cacheDatabaseProvider = cacheDatabaseProvider;
        _options = options;
        _recipeSettingViewModel = recipeSettingViewModel;
        _applicationCookie = applicationCookie;
        _calibrationSetting = calibrationSetting;
        messenger.RegisterAll(this);
    }

    [RelayCommand]
    private async Task LoadedAsync()
    {
        try
        {
            RecipeInfoDtoItems = [];
            var resultList = await _sysRecipeInformationService.GetAllAsync().ConfigureAwait(false);
            if (resultList.Count == 0)
            {
                _cacheDatabaseProvider.ChangeDatabase(_options.Value.NosqlDbDataSource, CancellationToken.None);

                _recipeCacheProvider.TryGetOrDefault<CalibrationRecipeDto>(out var calibrationRecipeDto);

                var defaultRecipeInfo = calibrationRecipeDto.CalibrationRecipeInfoDto.AdaptTo();

                var backupFilePath = Path.Combine(_options.Value.NosqlDbDataSourceDirectory, defaultRecipeInfo.RecipeDbName, Path.GetFileName(_options.Value.NosqlDbDataSource));

                if (System.IO.File.Exists(backupFilePath) == false)
                {
                    DirectoryHelper.CreateDirectoryIfNotExists(Path.GetDirectoryName(backupFilePath));
                    System.IO.File.Copy(_options.Value.NosqlDbDataSource, backupFilePath);
                }

                defaultRecipeInfo.RecipeNosqlRecipeDbDataSource = backupFilePath;

                await _sysRecipeInformationService.InsertAsync(defaultRecipeInfo).ConfigureAwait(false);

                resultList = await _sysRecipeInformationService.GetAllAsync().ConfigureAwait(false);
            }

            RecipeInfoDtoItems = [.. resultList.Where(t => t.IsDeleted == false)];
        }
        catch (Exception ex)
        {
            _dialogWindowProvider.ShowDialog($"Load recipe list failed! {ex.Message}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
        }
    }

    [RelayCommand]
    private Task UpdateAsync(object selectItemObj)
    {
        return Task.Run(() =>
        {
            try
            {
                if (selectItemObj is not SysRecipeInformationDto selectItem)
                {
                    _dialogWindowProvider.ShowDialog("Select the recipe failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    return;
                }

                SelectRecipeInfoDto = selectItem.Clone();
                var calibrationRecipeDto = new CalibrationRecipeDto();
                if (SelectRecipeInfoDto!.RecipeNosqlRecipeDbDataSource != string.Empty)
                {
                    _cacheDatabaseProvider.ChangeDatabase(SelectRecipeInfoDto!.RecipeNosqlRecipeDbDataSource, CancellationToken.None);

                    _recipeCacheProvider.TryGetOrDefault(out calibrationRecipeDto);
                }

                _recipeSettingViewModel.CalibrationRecipeDto = calibrationRecipeDto.Clone();

                _recipeSettingViewModel.CalibrationRecipeDto.CalibrationRecipeInfoDto.Id = SelectRecipeInfoDto.Id;

                _windowManagerService.ShowWindow(_recipeSettingViewModel);
            }
            catch (Exception ex)
            {
                _dialogWindowProvider.ShowDialog($"Update recipe failed! {ex.Message}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            }
        });
    }

    [RelayCommand]
    private async Task DeleteAsync(object selectItemObj)
    {
        try
        {
            if (selectItemObj is not SysRecipeInformationDto selectItem)
            {
                _dialogWindowProvider.ShowDialog("Select the recipe failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return;
            }

            SelectRecipeInfoDto = selectItem.Clone();

            if (ApplicationCookie.CalibrationRecipeDto is not null && SelectRecipeInfoDto.RecipeDbName.Equals(ApplicationCookie.CalibrationRecipeDto.CalibrationRecipeInfoDto.RecipeName))
            {
                _dialogWindowProvider.ShowDialog("unable to delete because of the selected recipe is currently applied, please select another recipe before deleting!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return;
            }

            _dialogWindowProvider.TryShowDialog($"Do you need to delete recipe '{SelectRecipeInfoDto.RecipeDbName}'?", out var dialogResultEnum, DialogButtonsEnum.YesNo, DialogIconEnum.Question);
            if (dialogResultEnum != DialogResultEnum.Yes)
                return;

            if (await _sysRecipeInformationService.DeleteAsync(SelectRecipeInfoDto).ConfigureAwait(false) == false)
            {
                _dialogWindowProvider.ShowDialog("Delete recipe failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return;
            }

            var deleteDbDirectoryPath = Path.GetDirectoryName(SelectRecipeInfoDto.RecipeNosqlRecipeDbDataSource);
            var deleteName = $"{SelectRecipeInfoDto.RecipeDbName}_delete";
            var parentPath = Path.GetDirectoryName(deleteDbDirectoryPath);
            var deletePath = deleteDbDirectoryPath.Replace(SelectRecipeInfoDto.RecipeDbName, deleteName);
            var newPath = FolderHelper.GenerateIndexedDirectoryPath(deletePath, parentPath);
            Directory.Move(deleteDbDirectoryPath, newPath);

            await LoadedAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _dialogWindowProvider.ShowDialog($"Delete recipe failed! {ex.Message}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
        }
        finally
        {
            RestoreApplyDatabase();
        }
    }

    [RelayCommand]
    private async Task InsertAsync()
    {
        try
        {
            RecipeInfoDtoItems ??= [];
            _contextProvider.Send(() => { RecipeInfoDtoItems.Add(new SysRecipeInformationDto()); });
            SelectRecipeInfoDto = RecipeInfoDtoItems.Last();
            _dialogWindowProvider.ShowDialog("After creating a new recipe, please edit and save it!");
            await UpdateAsync(SelectRecipeInfoDto);
        }
        catch (Exception ex)
        {
            _dialogWindowProvider.ShowDialog($"Insert recipe failed! {ex.Message}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
        }
    }

    [RelayCommand]
    private Task InheritAsync(object selectItemObj)
    {
        return Task.Run(async () =>
        {
            try
            {
                if (selectItemObj is not SysRecipeInformationDto selectItem)
                {
                    _dialogWindowProvider.ShowDialog("Select the recipe failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    return;
                }

                SelectRecipeInfoDto = selectItem.Clone();

                _dialogWindowProvider.TryShowDialog($"Do you need to add a new recipe and inherit recipe '{SelectRecipeInfoDto.RecipeDbName}'?", out var dialogResultEnum, DialogButtonsEnum.OKCancel, DialogIconEnum.Question);
                if (dialogResultEnum != DialogResultEnum.OK)
                    return;

                RecipeInfoDtoItems ??= [];
                var recipeInfoDtoBackUp = SelectRecipeInfoDto.Clone();
                recipeInfoDtoBackUp.RecipeDbName = $"{recipeInfoDtoBackUp.RecipeDbName}_backup";
                var resultList = await _sysRecipeInformationService.GetByConditionAsync(recipeInfoDtoBackUp).ConfigureAwait(false);
                if (resultList.Count > 0)
                {
                    _dialogWindowProvider.ShowDialog("The recipe name already exists!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    return;
                }

                recipeInfoDtoBackUp.RecipeNosqlRecipeDbDataSource = recipeInfoDtoBackUp.RecipeNosqlRecipeDbDataSource.Replace(SelectRecipeInfoDto.RecipeDbName, recipeInfoDtoBackUp.RecipeDbName);

                await CopyRecipeDatabaseAsync(SelectRecipeInfoDto.RecipeNosqlRecipeDbDataSource, recipeInfoDtoBackUp).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _dialogWindowProvider.ShowDialog($"Inherit recipe failed! {ex.Message}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            }
            finally
            {
                RestoreApplyDatabase();
            }
        });
    }

    [RelayCommand]
    private Task ImportAsync()
    {
        return Task.Run(async () =>
        {
            try
            {
                if (_dialogWindowProvider.TryShowSelectFilePathDialog(".db", out var filePath) == false)
                    return;
                _cacheDatabaseProvider.ChangeDatabase(filePath, CancellationToken.None);

                _recipeCacheProvider.TryGetOrDefault<CalibrationRecipeDto>(out var calibrationRecipeDto);
                var recipeInfoDto = calibrationRecipeDto.CalibrationRecipeInfoDto.AdaptTo();
                var resultList = await _sysRecipeInformationService.GetByConditionAsync(recipeInfoDto).ConfigureAwait(false);
                if (resultList.Count > 0)
                {
                    _dialogWindowProvider.TryShowDialog("The recipe name already exists!If yes,will automatically rename!", out DialogResultEnum dialogResultEnum, DialogButtonsEnum.YesNo, DialogIconEnum.Question);
                    if (dialogResultEnum == DialogResultEnum.No) return;

                    calibrationRecipeDto.CalibrationRecipeInfoDto.RecipeName = $"{recipeInfoDto.RecipeDbName}_Import";
                    calibrationRecipeDto.CalibrationRecipeInfoDto.RecipeNosqlRecipeDbDataSource = recipeInfoDto.RecipeNosqlRecipeDbDataSource.Replace(recipeInfoDto.RecipeDbName, calibrationRecipeDto.CalibrationRecipeInfoDto.RecipeName);
                    recipeInfoDto = calibrationRecipeDto.CalibrationRecipeInfoDto.AdaptTo();
                }

                await CopyRecipeDatabaseAsync(filePath, recipeInfoDto).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _dialogWindowProvider.ShowDialog($"Import recipe failed! {ex.Message}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            }
            finally
            {
                RestoreApplyDatabase();
            }
        });
    }

    [RelayCommand]
    private Task ApplyRecipeAsync()
    {
        return Task.Run(() =>
        {
            try
            {
                if (SelectRecipeInfoDto is null)
                {
                    _dialogWindowProvider.ShowDialog("Please Select a Recipe!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    return;
                }

                _cacheDatabaseProvider.ChangeDatabase(SelectRecipeInfoDto!.RecipeNosqlRecipeDbDataSource, CancellationToken.None);

                _recipeCacheProvider.TryGetOrDefault<CalibrationRecipeDto>(out var calibrationRecipeDto);
                ApplicationCookie.CalibrationRecipeDto = calibrationRecipeDto.Clone();
                ApplicationCookie.CalibrationReviseRecipeDto = calibrationRecipeDto.Clone();

                calibrationRecipeDto.CalibrationRecipeInfoDto.RecipeNosqlRecipeDbDataSource = SelectRecipeInfoDto.RecipeNosqlRecipeDbDataSource;
                _recipeCacheProvider.Set(calibrationRecipeDto, CancellationToken.None);

                //_calibrationSetting.AdaptIn(_recipeCacheProvider.GetOrDefault<CalibrationSetting>());
                Close();
                _messenger.Send(ToggleCalibrateEventFactory.RefreshMenuStatus(true)); // 刷新界面
            }
            catch (Exception ex)
            {
                _dialogWindowProvider.ShowDialog($"Apply recipe failed! {ex.Message}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            }
        });
    }

    [RelayCommand]
    private void Close()
    {
        if (ApplicationCookie.CalibrationRecipeDto is null)
        {
            _dialogWindowProvider.TryShowDialog("The recipe is not currently applied, please select again!", out var dialogResultEnum, DialogButtonsEnum.RetryCancel, DialogIconEnum.Warning);
            if (dialogResultEnum == DialogResultEnum.Retry)
                return;
        }

        CloseView(true);
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
                    OnPropertyChanged(nameof(ApplicationCookie));
                }
            }
        });
    }

    private async Task CopyRecipeDatabaseAsync(string oldDbDataSource, SysRecipeInformationDto newRecipeInformationDto)
    {
        try
        {
            RecipeInfoDtoItems ??= [];
            if (System.IO.File.Exists(newRecipeInformationDto.RecipeNosqlRecipeDbDataSource) == false)
            {
                DirectoryHelper.CreateDirectoryIfNotExists(Path.GetDirectoryName(newRecipeInformationDto.RecipeNosqlRecipeDbDataSource));

                System.IO.File.Copy(oldDbDataSource, newRecipeInformationDto.RecipeNosqlRecipeDbDataSource);

                newRecipeInformationDto.Id = 0;
                await _sysRecipeInformationService.InsertAsync(newRecipeInformationDto).ConfigureAwait(false);

                UpdateCalibrationRecipeDto(newRecipeInformationDto);

                _contextProvider.Send(() => { RecipeInfoDtoItems.Add(newRecipeInformationDto); });
                SelectRecipeInfoDto = RecipeInfoDtoItems.Last();
            }
            else
            {
                _dialogWindowProvider.ShowDialog("The recipe database already exists!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return;
            }

            await LoadedAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _dialogWindowProvider.ShowDialog($"Copy recipe database failed! {ex.Message}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
        }

        void UpdateCalibrationRecipeDto(SysRecipeInformationDto recipeInfoDto)
        {
            _cacheDatabaseProvider.ChangeDatabase(recipeInfoDto.RecipeNosqlRecipeDbDataSource, CancellationToken.None);

            _recipeCacheProvider.TryGetOrDefault<CalibrationRecipeDto>(out var calibrationRecipeDto);
            calibrationRecipeDto.CalibrationRecipeInfoDto.RecipeName = recipeInfoDto.RecipeDbName;
            calibrationRecipeDto.CalibrationRecipeInfoDto.RecipeNosqlRecipeDbDataSource = recipeInfoDto.RecipeNosqlRecipeDbDataSource;
            _recipeCacheProvider.Set(calibrationRecipeDto, CancellationToken.None);
        }
    }

    private void RestoreApplyDatabase()
    {
        try
        {
            if (ApplicationCookie.CalibrationRecipeDto is null)
            {
                _dialogWindowProvider.ShowDialog("Get apply lite database failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return;
            }

            _cacheDatabaseProvider.ChangeDatabase(ApplicationCookie.CalibrationRecipeDto.CalibrationRecipeInfoDto.RecipeNosqlRecipeDbDataSource, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _dialogWindowProvider.ShowDialog($"Restore apply database failed! {ex.Message}", DialogButtonsEnum.OK, DialogIconEnum.Warning);
        }
    }
}
using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Core.Models.Events;
using Core.Models.Helper;
using Core.Models.Models;
using Core.Wcf.Models;
using CugaCalibration.Core.Services.Interfaces;
using Local.SQL.Cache.Providers.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.IOC.Providers;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using System.Collections.ObjectModel;
using System.IO;

namespace CugaCalibration.ViewModels.Common.Windows.File.Setting.Children;

[IOCAppService(ServiceType = typeof(SettingCalibrateItemsStatusViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Transient)]
public sealed partial class SettingCalibrateItemsStatusViewModel : ViewModelBase
{
    private readonly IMessenger _messenger;
    private readonly IDialogWindowProvider _dialogWindowProvider;
    private readonly IGetResultFileService _getResultFileService;
    private readonly ILogger<SettingCalibrateItemsStatusViewModel> _logger;
    private readonly ICacheProvider _cacheProvider;
    private readonly ISynchronizationContextProvider _synchronizationContextProvider;
    private readonly ICalibrationCacheProvider _calibrationCacheProviderService;
    private readonly ICalibrationVersionFactory _calibrationVersionFactory;
    private readonly ConfigViewModel _configViewModel;
    private bool _isLoadSuccess;

    /// <summary>
    /// 反序列化wcf对象
    /// </summary>
    private CalibrationObj? _calibrationObj;

    public record CalibrationCategory(string Description, IReadOnlyList<CalibrationCategoryItem> Items);

    [ObservableProperty]
    public partial ObservableCollection<CalibrationCategory> CalibrationCategories { get; set; } = [];

    public SettingCalibrateItemsStatusViewModel(
        IMessenger messenger,
        IDialogWindowProvider dialogWindowProvider,
        IGetResultFileService getResultFileService,
        ILogger<SettingCalibrateItemsStatusViewModel> logger,
        ICacheProvider cacheProvider,
        ISynchronizationContextProvider synchronizationContextProvider,
        ICalibrationCacheProvider calibrationCacheProviderService,
        ICalibrationVersionFactory calibrationVersionFactory,
        ConfigViewModel configureViewModel)
    {
        _messenger = messenger;
        _dialogWindowProvider = dialogWindowProvider;
        _getResultFileService = getResultFileService;
        _logger = logger;
        _cacheProvider = cacheProvider;
        _synchronizationContextProvider = synchronizationContextProvider;
        _calibrationCacheProviderService = calibrationCacheProviderService;
        _calibrationVersionFactory = calibrationVersionFactory;
        _configViewModel = configureViewModel;

        _messenger.RegisterAll(this);
    }

    [RelayCommand]
    private async Task LoadedAsync()
    {
        await Task.Run(() =>
        {
            try
            {
                var appliedFilePath = _configViewModel.GetAppliedCalibrateResultFilePath();
                _getResultFileService.SetResultFilePath(appliedFilePath);

                if (_calibrationObj is not null) return; // 防止重复加载

                if (_getResultFileService.TryGet<CalibrationObj>(out var tempObj) == false)
                {
                    _dialogWindowProvider.ShowDialog("Load Calibration Result File Failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    return;
                }

                _calibrationObj = tempObj;

                if (GetCalibrationObjIsOkStatus() == false) return;

                _isLoadSuccess = true;
            }
            catch (Exception ex)
            {
                _isLoadSuccess = false;
                _logger.LogError(ex, "Get applied calibrate result file path failed!");
                _dialogWindowProvider.ShowDialog("Get applied calibrate result file path failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            }
        });
        return;

        bool GetCalibrationObjIsOkStatus()
        {
            try
            {
                _synchronizationContextProvider.Post(CalibrationCategories.Clear);

                var calibrationCategoryList = CalibrationReflectionHelper.GetCalibrationDescriptionList();

                foreach (var calibrationCategory in calibrationCategoryList)
                {
                    var items = new List<CalibrationCategoryItem>();
                    var calibrationCategoryObj = new CalibrationCategory(calibrationCategory.Description, items);

                    foreach (var calibrationCategoryItem in calibrationCategory.Items)
                    {
                        var calibrationCategoryItemObj = new CalibrationCategoryItem
                        {
                            Description = Guard.IsNotNullAndReturn(calibrationCategoryItem.CalibrationDtoType.Namespace).Split('.').Last(),
                            IsAnyOk = false,
                            IsArray = calibrationCategoryItem.IsArray,
                            Type = calibrationCategoryItem.CalibrationDtoType
                        };
                        if (calibrationCategoryItem.IsArray)
                        {
                            var calibrationDtoItems = _cacheProvider.GetArray(calibrationCategoryItem.CalibrationDtoType);
                            calibrationCategoryItemObj.IsAnyOk = calibrationDtoItems is not null && calibrationDtoItems.Length > 0;
                        }
                        else
                        {
                            var calibrationDto = _cacheProvider.Get(calibrationCategoryItem.CalibrationDtoType) as CalibrationDTOBase;
                            calibrationCategoryItemObj.IsAnyOk = calibrationDto?.IsCalibrated ?? false;
                        }

                        items.Add(calibrationCategoryItemObj);
                    }

                    _synchronizationContextProvider.Post(() => CalibrationCategories.Add(calibrationCategoryObj));
                }

                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Get CalibrationObj IsOk Status Failed!");
                return false;
            }
        }
    }

    public Task<bool> SavingAsync()
    {
        return Task.Run(async () =>
        {
            if (_isLoadSuccess == false) return true; // 未加载缓存成功，不保存

            var isChanged = false;
            foreach (var calibrationCategory in CalibrationCategories)
            {
                isChanged = calibrationCategory.Items.Any(t => t.IsChanged);
                if (isChanged) break;
            }

            if (isChanged == false) return true; // 未修改，不保存

            var showDialog = _dialogWindowProvider.TryShowDialog("Do you want to save the calibration enabled status settings? " +
                                                                 "  PS:If you select Yes, the calibration cache will be overwritten by Cuga's currently applied calibration file!",
                out var dialogResultEnum, DialogButtonsEnum.YesNo, DialogIconEnum.Question);
            if (showDialog == false || dialogResultEnum == DialogResultEnum.No)
            {
                Closing();
                return true;
            }

            Guard.IsNotNull(_calibrationObj);

            // 备份result
            if (_getResultFileService.TrySaveBackUp(_calibrationObj) == false)
            {
                _logger.LogError("Save BackUp Result File Failed!");
                return false;
            }

            // 禁用项写入db
            foreach (var calibrationCategory in CalibrationCategories)
            {
                foreach (var calibrationCategoryItem in calibrationCategory.Items)
                {
                    calibrationCategoryItem.Save(_cacheProvider, CancellationToken.None);
                }
            }

            // 序列化覆盖原先的result
            var appliedFilePath = _configViewModel.GetAppliedCalibrateResultFilePath();

            var calibrationVersionDTO = _calibrationVersionFactory.CreateInstanceFromCurrentDatabase(Path.GetFileName(appliedFilePath));

            var save = await _calibrationCacheProviderService.TrySaveAsync(calibrationVersionDTO, CancellationToken.None);
            if (save)
                _dialogWindowProvider.ShowDialog("Save Success.");
            else
                _dialogWindowProvider.ShowDialog("Save Failed! Please save it again.", DialogButtonsEnum.OK, DialogIconEnum.Error);

            _messenger.Send(ToggleCalibrateEventFactory.RefreshWindow(true)); // 刷新界面
            return true;
        });
    }

    public bool Closing()
    {
        _calibrationObj = null;

        return true;
    }
}

public partial class CalibrationCategoryItem : ObservableObject
{
    [ObservableProperty]
    public partial string Description { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsAnyOk { get; set; }

    public bool IsArray { get; init; }
    public Type? Type { get; init; }

    public bool IsChanged { get; private set; }

    partial void OnIsAnyOkChanged(bool value)
    {
        if (value == false) IsChanged = true;
        else IsAnyOk = true;
    }

    public void Save(ICacheProvider cacheProvider, CancellationToken cancellationToken)
    {
        if (IsChanged == false) return;

        if (IsArray)
        {
            if (Type is not null)
                cacheProvider.SetArray(Type, [], cancellationToken);
        }
        else
        {
            if (Type is not null)
                cacheProvider.Set(Type, Activator.CreateInstance(Type) ?? new object(), cancellationToken);
        }
    }
}
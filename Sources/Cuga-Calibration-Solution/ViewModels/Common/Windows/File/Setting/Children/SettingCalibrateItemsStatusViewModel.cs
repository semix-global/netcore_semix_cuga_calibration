using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Core.Models.Events;
using Core.Models.Models.Ads.PressureGains;
using Core.Models.Models.Ads.XGains;
using Core.Models.Models.Ads.YGains;
using Core.Models.Models.Chuck.Center;
using Core.Models.Models.Chuck.Gantry;
using Core.Models.Models.Chuck.GlobalScaleError;
using Core.Models.Models.Chuck.Prealigner;
using Core.Models.Models.Chuck.RotateScaleError;
using Core.Models.Models.Chuck.StageMap;
using Core.Models.Models.Laser.AodDelay;
using Core.Models.Models.Laser.Attenuator;
using Core.Models.Models.Laser.AutoFocus;
using Core.Models.Models.Laser.IlluminationProfile;
using Core.Models.Models.Laser.LineCentricity;
using Core.Models.Models.Laser.OpticalPower;
using Core.Models.Models.Laser.PixelSize;
using Core.Models.Models.Laser.PmtAgcDelay;
using Core.Models.Models.Laser.XPixelSize;
using Core.Models.Models.Laser.XTCCalibration;
using Core.Models.Models.Laser.XYAstigmatism;
using Core.Models.Models.Microscope.CalChip;
using Core.Models.Models.Microscope.Centricity;
using Core.Models.Models.Microscope.Focus;
using Core.Models.Models.Microscope.PixelSize;
using Core.Wcf.Models;
using CugaCalibration.Core.Services.Interfaces;
using Local.NoSQL.DB.Providers.Interfaces;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.IOC.Providers;
using Net.Utilities.Models;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM.Providers;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;

namespace CugaCalibration.ViewModels.Common.Windows.File.Setting.Children;

[IOCAppService(ServiceType = typeof(SettingCalibrateItemsStatusViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Transient)]
public sealed partial class SettingCalibrateItemsStatusViewModel : SettingWindowViewModelBase
{
    private readonly IMessenger _messenger;
    private readonly IDialogWindowProvider _dialogWindowProvider;
    private readonly IGetResultFileService _getResultFileService;
    private readonly ILogger<SettingCalibrateItemsStatusViewModel> _logger;
    private readonly ICacheProvider _cacheProvider;
    private readonly ISynchronizationContextProvider _synchronizationContextProvider;
    private readonly ICalibrationCacheProvider _calibrationCacheProviderService;
    private readonly ConfigViewModel _configViewModel;
    private bool _isLoadSuccess;

    /// <summary>
    /// 反序列化wcf对象
    /// </summary>
    private CalibrationObj? _calibrationObj;

    public record CalibrationCategory(string Description, IReadOnlyList<CalibrationCategoryItem> Items);

    [ObservableProperty]
    private ObservableCollection<CalibrationCategory> _calibrationCategories = [];

    public SettingCalibrateItemsStatusViewModel
    (
        IMessenger messenger,
        IDialogWindowProvider dialogWindowProvider,
        IGetResultFileService getResultFileService,
        ILogger<SettingCalibrateItemsStatusViewModel> logger,
        ICacheProvider cacheProvider,
        ISynchronizationContextProvider synchronizationContextProvider,
        ICalibrationCacheProvider calibrationCacheProviderService,
        ConfigViewModel configureViewModel)
    {
        _messenger = messenger;
        _dialogWindowProvider = dialogWindowProvider;
        _getResultFileService = getResultFileService;
        _logger = logger;
        _cacheProvider = cacheProvider;
        _synchronizationContextProvider = synchronizationContextProvider;
        _calibrationCacheProviderService = calibrationCacheProviderService;
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
                _synchronizationContextProvider.Send(CalibrationCategories.Clear);
                foreach (var propertyInfo in typeof(CalibrationObj).GetProperties())
                {
                    var calibrationCategoryValue = GuardUtils.IsNotNullAndReturn(propertyInfo.GetValue(_calibrationObj));

                    var items = new List<CalibrationCategoryItem>();
                    var calibrationCategory = new CalibrationCategory(propertyInfo.GetCustomAttribute<DescriptionAttribute>()!.Description, items);

                    foreach (var property in propertyInfo.PropertyType.GetProperties())
                    {
                        var calibrationCategoryItemValue = GuardUtils.IsNotNullAndReturn(property.GetValue(calibrationCategoryValue));

                        var isArray = calibrationCategoryItemValue is IEnumerable<CalibrationBase>;
                        if (isArray)
                        {
                            var calibrationBases = GuardUtils.IsNotNullAndReturn(calibrationCategoryItemValue as IEnumerable<CalibrationBase>).ToArray();

                            items.Add(new CalibrationCategoryItem(property.GetCustomAttribute<DescriptionAttribute>()!.Description, calibrationBases.Length > 0, true));
                        }
                        else
                        {
                            var calibrationBase = GuardUtils.IsNotNullAndReturn(calibrationCategoryItemValue as CalibrationBase);

                            items.Add(new CalibrationCategoryItem(property.GetCustomAttribute<DescriptionAttribute>()!.Description, calibrationBase.IsOk, false));
                        }
                    }

                    _synchronizationContextProvider.Send(() => CalibrationCategories.Add(calibrationCategory));
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

    public override Task<bool> SavingAsync()
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

            Guard.IsNotNull(_calibrationObj, nameof(_calibrationObj));

            if (_getResultFileService.TrySaveBackUp(_calibrationObj) == false) // 备份result
            {
                _logger.LogError("Save BackUp Result File Failed!");
                return false;
            }

            foreach (var calibrationCategory in CalibrationCategories)
            {
                switch (calibrationCategory.Description)
                {
                    case WcfConstantHelper.AdsNodeCalibrationName:
                        foreach (var calibrationCategoryItem in calibrationCategory.Items)
                        {
                            switch (calibrationCategoryItem.Description)
                            {
                                case WcfConstantHelper.AdsXGainCalibrationName:
                                    calibrationCategoryItem.Save<AdsXGainsItemDto>(_cacheProvider, CancellationToken.None);
                                    break;
                                case WcfConstantHelper.AdsYGainCalibrationName:
                                    calibrationCategoryItem.Save<AdsYGainsItemDto>(_cacheProvider, CancellationToken.None);
                                    break;
                                case WcfConstantHelper.AdsPressureCalibrationName:
                                    calibrationCategoryItem.Save<AdsPressureGainsDto>(_cacheProvider, CancellationToken.None);
                                    break;
                            }
                        }

                        break;
                    case WcfConstantHelper.MicroscopeNodeCalibrationName:
                        foreach (var calibrationCategoryItem in calibrationCategory.Items)
                        {
                            switch (calibrationCategoryItem.Description)
                            {
                                case WcfConstantHelper.MicroscopeFocusCalibrationName:
                                    calibrationCategoryItem.Save<MicroscopeFocusItemDto>(_cacheProvider, CancellationToken.None);
                                    break;
                                case WcfConstantHelper.MicroscopePixelSizeCalibrationName:
                                    calibrationCategoryItem.Save<MicroscopePixelSizeItemDto>(_cacheProvider, CancellationToken.None);
                                    break;
                                case WcfConstantHelper.MicroscopeCentricityCalibrationName:
                                    calibrationCategoryItem.Save<MicroscopeCentricityItemDto>(_cacheProvider, CancellationToken.None);
                                    break;
                                case WcfConstantHelper.MicroscopeCalChipCalibrationName:
                                    calibrationCategoryItem.Save<MicroscopeCalChipDto>(_cacheProvider, CancellationToken.None);
                                    break;
                            }
                        }

                        break;
                    case WcfConstantHelper.ChuckNodeCalibrationName:
                        foreach (var calibrationCategoryItem in calibrationCategory.Items)
                        {
                            switch (calibrationCategoryItem.Description)
                            {
                                case WcfConstantHelper.ChuckGantryCalibrationName:
                                    calibrationCategoryItem.Save<ChuckGantryDto>(_cacheProvider, CancellationToken.None);
                                    break;
                                case WcfConstantHelper.ChuckCenterCalibrationName:
                                    calibrationCategoryItem.Save<ChuckCenterObjDto>(_cacheProvider, CancellationToken.None);
                                    break;
                                case WcfConstantHelper.ChuckPrealignerCalibrationName:
                                    calibrationCategoryItem.Save<ChuckPrealignerObjDto>(_cacheProvider, CancellationToken.None);
                                    break;
                                case WcfConstantHelper.ChuckStageMapCalibrationName:
                                    calibrationCategoryItem.Save<ChuckStageMapDto>(_cacheProvider, CancellationToken.None);
                                    break;
                                case WcfConstantHelper.ChuckGlobalScaleErrorCalibrationName:
                                    calibrationCategoryItem.Save<ChuckGlobalScaleErrorDto>(_cacheProvider, CancellationToken.None);
                                    break;
                                case WcfConstantHelper.ChuckRotateScaleErrorCalibrationName:
                                    calibrationCategoryItem.Save<ChuckRotateScaleErrorDto>(_cacheProvider, CancellationToken.None);
                                    break;
                            }
                        }

                        break;
                    case WcfConstantHelper.LaserNodeCalibrationName:
                        foreach (var calibrationCategoryItem in calibrationCategory.Items)
                        {
                            switch (calibrationCategoryItem.Description)
                            {
                                case WcfConstantHelper.LaserAutoFocusCalibrationName:
                                    calibrationCategoryItem.Save<LaserAutoFocusDto>(_cacheProvider, CancellationToken.None);
                                    break;
                                case WcfConstantHelper.LaserPixelSizeCalibrationName:
                                    calibrationCategoryItem.Save<LaserPixelSizeItemDto>(_cacheProvider, CancellationToken.None);
                                    break;
                                case WcfConstantHelper.LaserLineCentricityCalibrationName:
                                    calibrationCategoryItem.Save<LaserLineCentricityItemDto>(_cacheProvider, CancellationToken.None);
                                    break;
                                case WcfConstantHelper.LaserAodDelayCalibrationName:
                                    calibrationCategoryItem.Save<LaserAodDelayItemDto>(_cacheProvider, CancellationToken.None);
                                    break;
                                case WcfConstantHelper.LaserIlluminationProfileCalibrationName:
                                    calibrationCategoryItem.Save<LaserIlluminationProfileItemDto>(_cacheProvider, CancellationToken.None);
                                    break;
                                case WcfConstantHelper.LaserOpticalPowerCalibrationName:
                                    calibrationCategoryItem.Save<LaserOpticalPowerDto>(_cacheProvider, CancellationToken.None);
                                    break;
                                case WcfConstantHelper.LaserAttenuatorCalibrationName:
                                    calibrationCategoryItem.Save<LaserAttenuatorObjDto>(_cacheProvider, CancellationToken.None);
                                    break;
                                case WcfConstantHelper.LaserXyAstigmatismCalibrationName:
                                    calibrationCategoryItem.Save<LaserXYAstigmatismCalibrationItemDto>(_cacheProvider, CancellationToken.None);
                                    break;
                                case WcfConstantHelper.LaserXPixelSizeCalibrationName:
                                    calibrationCategoryItem.Save<LaserXPixelSizeItemDto>(_cacheProvider, CancellationToken.None);
                                    break;
                                case WcfConstantHelper.LaserXtcCalibrationName:
                                    calibrationCategoryItem.Save<LaserXTCCalibrationItemDto>(_cacheProvider, CancellationToken.None);
                                    break;
                                case WcfConstantHelper.LaserAgcDelayCalibrationName:
                                    calibrationCategoryItem.Save<LaserPmtAgcDelayItemDto>(_cacheProvider, CancellationToken.None);
                                    break;
                            }
                        }

                        break;
                }
            }

            // 序列化
            var save = await _calibrationCacheProviderService.TrySaveAsync().ConfigureAwait(false);
            if (save)
                _dialogWindowProvider.ShowDialog("Save Success.");
            else
                _dialogWindowProvider.ShowDialog("Save Failed! Please save it again.", DialogButtonsEnum.OK, DialogIconEnum.Error);

            _messenger.Send(ToggleCalibrateEventFactory.RefreshMenuStatus(true)); // 刷新界面
            return true;
        });
    }

    public override bool Closing()
    {
        _calibrationObj = null;
        return true;
    }
}

public partial class CalibrationCategoryItem(string description, bool isAnyOk, bool isArray) : ObservableObject
{
    [ObservableProperty]
    private string _description = description;

    [ObservableProperty]
    private bool _isAnyOk = isAnyOk;

    public bool IsChanged { get; private set; }

    partial void OnIsAnyOkChanged(bool value)
    {
        if (value == false) IsChanged = true;
        else IsAnyOk = true;
    }

    public void Save<T>(ICacheProvider cacheProvider, CancellationToken cancellationToken) where T : class, ICacheItem, new()
    {
        if (IsChanged == false) return;

        if (isArray) cacheProvider.SetArray<T>([], cancellationToken);
        else cacheProvider.Set(new T(), cancellationToken);
    }
}
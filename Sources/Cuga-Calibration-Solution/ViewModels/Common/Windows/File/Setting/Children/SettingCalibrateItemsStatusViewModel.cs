using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Core.Models.Events;
using Core.Models.Extensions;
using Core.Models.Models.Ads.PressureGains;
using Core.Models.Models.Ads.XGains;
using Core.Models.Models.Ads.YGains;
using Core.Models.Models.Chuck.Center;
using Core.Models.Models.Chuck.DarkFieldStageMap;
using Core.Models.Models.Chuck.Gantry;
using Core.Models.Models.Chuck.GlobalScaleError;
using Core.Models.Models.Chuck.Prealigner;
using Core.Models.Models.Chuck.RotateScaleError;
using Core.Models.Models.Laser.AodDelay;
using Core.Models.Models.Laser.AutoFocus;
using Core.Models.Models.Laser.IlluminationProfile;
using Core.Models.Models.Laser.LineCentricity;
using Core.Models.Models.Laser.OpticalPower;
using Core.Models.Models.Laser.PixelSize;
using Core.Models.Models.Laser.XPixelSize;
using Core.Models.Models.Laser.XTCCalibration;
using Core.Models.Models.Laser.XYAstigmatism;
using Core.Models.Models.Microscope.CalChip;
using Core.Models.Models.Microscope.Centricity;
using Core.Models.Models.Microscope.Focus;
using Core.Models.Models.Microscope.PixelSize;
using Core.Wcf.Models;
using CugaCalibration.Core.Services.Interfaces;
using Microsoft.Extensions.Logging;
using MoreLinq;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Mapper.Interfaces;
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
    private readonly ICalibrationCacheProvider _calibrationCacheProviderService;
    private readonly ConfigViewModel _configViewModel;

    private bool _isLoadSuccess;

    /// <summary>
    /// wcfObj反射字典，key：校准大类 value：校准小类
    /// </summary>
    private readonly Dictionary<PropertyInfo, Dictionary<PropertyInfo, object>> _calibrateObjDictionary = [];

    /// <summary>
    /// 反序列化wcf对象
    /// </summary>
    private CalibrationObj? _calibrationObj;

    /// <summary>
    /// wcfObj转换后的界面binding source
    /// </summary>
    [ObservableProperty]
    private CalibrateStatus _calibrationStatus = new();

    /// <summary>
    /// 副本，用于save时判断是否有修改
    /// </summary>
    private CalibrateStatus? _calibrationStatusBackUp;

    public SettingCalibrateItemsStatusViewModel(IMessenger messenger, IDialogWindowProvider dialogWindowProvider, IGetResultFileService getResultFileService, ILogger<SettingCalibrateItemsStatusViewModel> logger,
        ICalibrationCacheProvider calibrationCacheProviderService, ConfigViewModel configureViewModel)
    {
        _messenger = messenger;
        _dialogWindowProvider = dialogWindowProvider;
        _getResultFileService = getResultFileService;
        _logger = logger;
        _calibrationCacheProviderService = calibrationCacheProviderService;
        _configViewModel = configureViewModel;

        _messenger.RegisterAll(this);
    }

    [RelayCommand]
    private async Task LoadedAsync() => await LoadCalibrationStatusAsync().ConfigureAwait(false);

    private Task LoadCalibrationStatusAsync()
    {
        return Task.Run(() =>
        {
            try
            {
                var appliedFilePath = _configViewModel.GetAppliedCalibrateResultFilePath();
                _getResultFileService.SetResultFilePath(appliedFilePath);

                if (_calibrationObj is not null && _calibrationStatusBackUp is not null && _calibrationStatusBackUp.Equals(CalibrationStatus)) return; // 防止重复加载

                if (_getResultFileService.TryGet<CalibrationObj>(out var tempObj) == false)
                {
                    _dialogWindowProvider.ShowDialog("Load Calibration Result File Failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                    return;
                }

                _calibrationObj = tempObj;
                _calibrateObjDictionary.Clear();
                if (GetCalibrationObjIsOkStatus() == false) return;

                _isLoadSuccess = true;
                _calibrationStatusBackUp ??= CalibrationStatus.Clone();
            }
            catch (Exception ex)
            {
                _isLoadSuccess = false;
                _logger.LogError(ex, "Get applied calibrate result file path failed!");
                _dialogWindowProvider.ShowDialog("Get applied calibrate result file path failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
            }
        });

        // wcf对象转换界面binding对象
        bool GetCalibrationObjIsOkStatus()
        {
            try
            {
                var parentStatusPropertyInfo = CalibrationStatus.GetType().GetProperties();
                var parentCalibrationStatusObjItems = parentStatusPropertyInfo.Select(t => (propertyInfo: t, value: t.GetValue(CalibrationStatus)))
                    .ToList();

                var parentWcfPropertyInfo = _calibrationObj.GetType().GetProperties();
                var parentWcfObjDictionary = parentWcfPropertyInfo.Select(t => (propertyInfo: t, value: t.GetValue(_calibrationObj)))
                    .ToDictionary(t => t.propertyInfo, t => t.value);

                parentWcfObjDictionary.ForEach(parentWcf =>
                {
                    var childWcfPropertyInfo = parentWcf.Value.GetType().GetProperties();
                    var childWcfDictionary = childWcfPropertyInfo.Select(t => (propertyInfo: t, value: t.GetValue(parentWcf.Value)))
                        .ToDictionary(t => t.propertyInfo, t => t.value);
                    _calibrateObjDictionary.Add(parentWcf.Key, childWcfDictionary);
                });

                _calibrateObjDictionary.ForEach(parentWcfObj =>
                {
                    var parentWcfAttribute = parentWcfObj.Key.GetCustomAttribute<DescriptionAttribute>().Description;
                    var childCalibrateStatusItems = new ObservableCollection<CalibrateStatusItems>(
                    [
                        .. parentWcfObj.Value.Select(childWcfObj =>
                        {
                            var childWcfTransObj = childWcfObj.Value.GetType().IsArray
                                ? ObjectConvertToList<CalibrationBase>(childWcfObj.Value)
                                : [(CalibrationBase)childWcfObj.Value];
                            return new CalibrateStatusItems
                            {
                                CalibrateItemName = childWcfObj.Key.GetCustomAttribute<DescriptionAttribute>().Description,
                                Enabled = childWcfTransObj.GetIsOkStatus()
                            };
                        })
                    ]);
                    var (statusPropertyInfo, _) = parentCalibrationStatusObjItems.SingleOrDefault(t => t.propertyInfo.GetCustomAttribute<DescriptionAttribute>().Description == parentWcfAttribute);
                    statusPropertyInfo.SetValue(CalibrationStatus, childCalibrateStatusItems);
                });
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Get CalibrationObj isOk status Failed!");
                _dialogWindowProvider.ShowDialog("Get CalibrationObj isOk status Failed!", DialogButtonsEnum.OK, DialogIconEnum.Warning);
                return false;
            }
        }
    }

    public override Task<bool> SavingAsync()
    {
        return Task.Run(() =>
        {
            if (_isLoadSuccess == false) return true; // 未加载缓存成功，不保存
            if (_calibrationStatusBackUp!.Equals(CalibrationStatus)) return true; // 无修改，不保存

            var showDialog = _dialogWindowProvider.TryShowDialog("Do you want to save the calibration enabled status settings? " +
                                                                 "  PS:If you select Yes, the calibration cache will be overwritten by Cuga's currently applied calibration file!",
                out var dialogResultEnum, DialogButtonsEnum.YesNo, DialogIconEnum.Question);
            if (showDialog == false || dialogResultEnum == DialogResultEnum.No) return true;

            if (_getResultFileService.TrySaveBackUp(_calibrationObj!) == false) // 备份result
            {
                _logger.LogError("Save BackUp Result File Failed!");
                return false;
            }

            if (ChangeDenpendCalibrationStatus(_calibrationStatusBackUp, CalibrationStatus) == false) return false; //禁用依赖项状态

            if (SetCalibrationObjIsOkStatus() == false) return false; //状态写回wcf obj

            if (SaveFileCache() == false) return false; //保存result文件、更新缓存数据库

            _messenger.Send(ToggleCalibrateEventFactory.RefreshMenuStatus(true)); // 刷新界面

            return true;
        });

        bool SaveFileCache()
        {
            try
            {
                var cancellationToken = CancellationToken.None;
                Guard.IsNotNull(_calibrationObj, nameof(_calibrationObj));

                if (_getResultFileService.TrySave(_calibrationObj) == false) return false;

                if (_calibrationObj.CalibrationAdsObj.CalibrationAdsPressureGains.IsOk)
                {
                    if (_calibrationCacheProviderService.TrySetDisable<AdsPressureGainsDto>(cancellationToken) == false) return false;
                }

                if (_calibrationObj.CalibrationAdsObj.CalibrationAdsXGains.IsOk)
                {
                    if (_calibrationCacheProviderService.TrySetDisable<AdsXGainsItemDto>(cancellationToken) == false) return false;
                }

                if (_calibrationObj.CalibrationAdsObj.CalibrationAdsYGains.IsOk)
                {
                    if (_calibrationCacheProviderService.TrySetDisable<AdsYGainsItemDto>(cancellationToken) == false) return false;
                }

                if (_calibrationObj.CalibrationMicroscopeObj.CalibrationMicroscopeFocusItemList.All(t => t.IsOk))
                {
                    if (_calibrationCacheProviderService.TrySetArrayDisable<MicroscopeFocusItemDto>(cancellationToken) == false) return false;
                }

                if (_calibrationObj.CalibrationMicroscopeObj.CalibrationMicroscopeCalChip.IsOk)
                {
                    if (_calibrationCacheProviderService.TrySetDisable<MicroscopeCalChipDto>(cancellationToken) == false) return false;
                }

                if (_calibrationObj.CalibrationMicroscopeObj.CalibrationMicroscopePixelSizeItemList.All(t => t.IsOk))
                {
                    if (_calibrationCacheProviderService.TrySetArrayDisable<MicroscopePixelSizeItemDto>(cancellationToken) == false) return false;
                }

                if (_calibrationObj.CalibrationMicroscopeObj.CalibrationMicroscopeCentricityItemList.All(t => t.IsOk))
                {
                    if (_calibrationCacheProviderService.TrySetArrayDisable<MicroscopeCentricityItemDto>(cancellationToken) == false) return false;
                }

                if (_calibrationObj.CalibrationChuckObj.CalibrationChuckGantry.IsOk)
                {
                    if (_calibrationCacheProviderService.TrySetDisable<ChuckGantryDto>(cancellationToken) == false) return false;
                }

                if (_calibrationObj.CalibrationChuckObj.CalibrationCenterObj.IsOk)
                {
                    if (_calibrationCacheProviderService.TrySetDisable<ChuckCenterObjDto>(cancellationToken) == false) return false;
                }

                if (_calibrationObj.CalibrationChuckObj.CalibrationPrealignerObj.IsOk)
                {
                    if (_calibrationCacheProviderService.TrySetDisable<ChuckPrealignerObjDto>(cancellationToken) == false) return false;
                }

                if (_calibrationObj.CalibrationChuckObj.CalibrationChuckDarkFieldStageMap.IsOk)
                {
                    if (_calibrationCacheProviderService.TrySetDisable<ChuckDarkFieldStageMapDto>(cancellationToken) == false) return false;
                }

                if (_calibrationObj.CalibrationChuckObj.CalibrationChuckGlobalScaleError.IsOk)
                {
                    if (_calibrationCacheProviderService.TrySetDisable<ChuckGlobalScaleErrorDto>(cancellationToken) == false) return false;
                }

                if (_calibrationObj.CalibrationChuckObj.CalibrationChuckRotateScaleError.IsOk)
                {
                    if (_calibrationCacheProviderService.TrySetDisable<ChuckRotateScaleErrorDto>(cancellationToken) == false) return false;
                }

                if (_calibrationObj.CalibrationLaserObj.CalibrationLaserAutoFocus.IsOk)
                {
                    if (_calibrationCacheProviderService.TrySetDisable<LaserAutoFocusDto>(cancellationToken) == false) return false;
                }

                if (_calibrationObj.CalibrationLaserObj.CalibrationLaserAodDelayItemList.All(t => t.IsOk))
                {
                    if (_calibrationCacheProviderService.TrySetArrayDisable<LaserAodDelayItemDto>(cancellationToken) == false) return false;
                }

                if (_calibrationObj.CalibrationLaserObj.CalibrationLaserXtcCalibrationItemList.All(t => t.IsOk))
                {
                    if (_calibrationCacheProviderService.TrySetArrayDisable<LaserXTCCalibrationItemDto>(cancellationToken) == false) return false;
                }

                if (_calibrationObj.CalibrationLaserObj.CalibrationLaserPixelSizeItemList.All(t => t.IsOk))
                {
                    if (_calibrationCacheProviderService.TrySetArrayDisable<LaserPixelSizeItemDto>(cancellationToken) == false) return false;
                }

                if (_calibrationObj.CalibrationLaserObj.CalibrationLaserXPixelSizeList.All(t => t.IsOk))
                {
                    if (_calibrationCacheProviderService.TrySetArrayDisable<LaserXPixelSizeItemDto>(cancellationToken) == false) return false;
                }

                if (_calibrationObj.CalibrationLaserObj.CalibrationLaserLineCentricityItemList.All(t => t.IsOk))
                {
                    if (_calibrationCacheProviderService.TrySetArrayDisable<LaserLineCentricityItemDto>(cancellationToken) == false) return false;
                }

                if (_calibrationObj.CalibrationLaserObj.CalibrationLaserIlluminationProfileItemList.All(t => t.IsOk))
                {
                    if (_calibrationCacheProviderService.TrySetArrayDisable<LaserIlluminationProfileItemDto>(cancellationToken) == false) return false;
                }

                if (_calibrationObj.CalibrationLaserObj.CalibrationLaserOpticalPowerList.All(t => t.IsOk))
                {
                    if (_calibrationCacheProviderService.TrySetArrayDisable<LaserOpticalPowerDto>(cancellationToken) == false) return false;
                }

                if (_calibrationObj.CalibrationLaserObj.CalibrationLaserXYAstigmatismItemList.All(t => t.IsOk))
                {
                    if (_calibrationCacheProviderService.TrySetArrayDisable<LaserXYAstigmatismCalibrationItemDto>(cancellationToken) == false) return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Save Result or Db File Failed!");
                return false;
            }
        }
    }

    public override bool Closing()
    {
        _calibrationObj = null;
        _calibrationStatusBackUp = null;
        return true;
    }

    /// <summary>
    /// 界面设置状态对象转换为wcf对象
    /// </summary>
    /// <returns></returns>
    private bool SetCalibrationObjIsOkStatus()
    {
        try
        {
            var properties = CalibrationStatus.GetType().GetProperties();
            foreach (var parent in _calibrateObjDictionary)
            {
                var parentWcfObj = parent.Key.GetValue(_calibrationObj);
                var parentAttributeName = parent.Key.GetCustomAttribute<DescriptionAttribute>().Description;
                foreach (var childDictionary in parent.Value)
                {
                    var childAttributeName = childDictionary.Key.GetCustomAttribute<DescriptionAttribute>().Description;
                    var childStatus = properties
                        .Where(t => t.GetCustomAttribute<DescriptionAttribute>().Description == parentAttributeName)
                        .Select(t => t.GetValue(CalibrationStatus))
                        .OfType<ObservableCollection<CalibrateStatusItems>>()
                        .Single()
                        .Single(t => t.CalibrateItemName == childAttributeName);
                    var childWcfObj = childDictionary.Key.GetValue(parentWcfObj);
                    var isSuccess = SetCalibrationItemIsOkStatus(childWcfObj, childStatus.Enabled);
                    if (isSuccess == false)
                        return false;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Set Calibration Obj Is Ok Status Failed!");
            return false;
        }

        static bool SetCalibrationItemIsOkStatus(object childCalibrationObj, bool isOk)
        {
            try
            {
                if (childCalibrationObj is System.Collections.ICollection)
                {
                    var childCalibrationObjList = ObjectConvertToList<CalibrationBase>(childCalibrationObj);
                    childCalibrationObjList.SetIsOkStatus(isOk);
                }
                else
                {
                    if (childCalibrationObj.GetType().IsSubclassOf(typeof(CalibrationBase)) == false)
                        return false;
                    (childCalibrationObj as CalibrationBase)!.SetIsOkStatus(isOk);
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    private bool ChangeDenpendCalibrationStatus(CalibrateStatus originStatus, CalibrateStatus currentStatus)
    {
        try
        {
            if (originStatus.Equals(currentStatus))
                return true;
            var changeItems = originStatus.GetType().GetProperties()
                .Select(t => t.GetValue(originStatus) as ObservableCollection<CalibrateStatusItems>)
                .SelectMany(t => t)
                .Where(oldItem => oldItem.Enabled)
                .Select(oldItem => currentStatus.GetType()
                    .GetProperties()
                    .Select(t => t.GetValue(currentStatus) as ObservableCollection<CalibrateStatusItems>)
                    .SelectMany(t => t)
                    .FirstOrDefault(newItem => newItem.CalibrateItemName == oldItem.CalibrateItemName && newItem.Enabled == false))
                .Where(t => t is not null)
                .ToList();
            var backupStatus = currentStatus.Clone();
            changeItems.ForEach(changeItem =>
            {
                if (changeItem is not null)
                    switch (changeItem.CalibrateItemName)
                    {
                        case WcfConstantHelper.MicroscopePixelSizeCalibrationName:
                            currentStatus.CalibrationMicroscopeObjStatus.ForEach(t =>
                            {
                                if (t.CalibrateItemName == WcfConstantHelper.MicroscopeCentricityCalibrationName)
                                    t.Enabled = false;
                            });
                            break;

                        case WcfConstantHelper.ChuckGantryCalibrationName:
                            currentStatus.CalibrationChuckObjStatus.ForEach(t =>
                            {
                                if (t.CalibrateItemName == WcfConstantHelper.ChuckGlobalScaleErrorCalibrationName)
                                    t.Enabled = false;
                            });
                            break;

                        case WcfConstantHelper.ChuckGlobalScaleErrorCalibrationName:
                            currentStatus.CalibrationChuckObjStatus.ForEach(t =>
                            {
                                if (t.CalibrateItemName == WcfConstantHelper.ChuckBrightFieldStageMapCalibrationName
                                    || t.CalibrateItemName == WcfConstantHelper.ChuckDarkFieldStageMapCalibrationName)
                                    t.Enabled = false;
                            });
                            break;

                        case WcfConstantHelper.LaserPixelSizeCalibrationName:
                            currentStatus.CalibrationLaserObjStatus.ForEach(t =>
                            {
                                if (t.CalibrateItemName == WcfConstantHelper.LaserLineCentricityCalibrationName)
                                    t.Enabled = false;
                            });
                            break;
                    }
            });
            if (backupStatus.Equals(currentStatus) == false)
                ChangeDenpendCalibrationStatus(backupStatus, currentStatus);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Change Denpend Calibration Status Failed!");
            return false;
        }
    }

    private static List<T> ObjectConvertToList<T>(object listObj)
    {
        var result = new List<T>();
        if (listObj.GetType().IsGenericType == false)
            return result;
        if (listObj is System.Collections.ICollection list)
        {
            if (list.Count > 0)
            {
                foreach (var item in list)
                {
                    result.Add((T)item);
                }
            }
        }
        else
            return result;

        return result;
    }

    public sealed partial class CalibrateStatus : ObservableObject, ICloneable<CalibrateStatus>
    {
        [ObservableProperty]
        [property: Description(WcfConstantHelper.AdsNodeCalibrationName)]
        private ObservableCollection<CalibrateStatusItems> _calibrationAdsObjStatus = [];

        [ObservableProperty]
        [property: Description(WcfConstantHelper.MicroscopeNodeCalibrationName)]
        private ObservableCollection<CalibrateStatusItems> _calibrationMicroscopeObjStatus = [];

        [ObservableProperty]
        [property: Description(WcfConstantHelper.ChuckNodeCalibrationName)]
        private ObservableCollection<CalibrateStatusItems> _calibrationChuckObjStatus = [];

        [ObservableProperty]
        [property: Description(WcfConstantHelper.LaserNodeCalibrationName)]
        private ObservableCollection<CalibrateStatusItems> _calibrationLaserObjStatus = [];

        public CalibrateStatus Clone() => new()
        {
            CalibrationAdsObjStatus = [.. CalibrationAdsObjStatus.Select(t => t.Clone()).ToList()],
            CalibrationMicroscopeObjStatus = [.. CalibrationMicroscopeObjStatus.Select(t => t.Clone()).ToList()],
            CalibrationChuckObjStatus = [.. CalibrationChuckObjStatus.Select(t => t.Clone()).ToList()],
            CalibrationLaserObjStatus = [.. CalibrationLaserObjStatus.Select(t => t.Clone()).ToList()]
        };

        public override bool Equals(object obj)
        {
            if (obj is not CalibrateStatus other)
                return false;
            return EqualsMethod(other);
        }

        private bool EqualsMethod(CalibrateStatus other)
        {
            return CalibrationAdsObjStatus.SequenceEqual(other.CalibrationAdsObjStatus)
                   && CalibrationMicroscopeObjStatus.SequenceEqual(other.CalibrationMicroscopeObjStatus)
                   && CalibrationChuckObjStatus.SequenceEqual(other.CalibrationChuckObjStatus)
                   && CalibrationLaserObjStatus.SequenceEqual(other.CalibrationLaserObjStatus);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(CalibrationAdsObjStatus, CalibrationMicroscopeObjStatus, CalibrationChuckObjStatus, CalibrationLaserObjStatus, CalibrationAdsObjStatus, CalibrationMicroscopeObjStatus, CalibrationChuckObjStatus, CalibrationLaserObjStatus);
        }
    }

    public sealed partial class CalibrateStatusItems : ObservableObject, ICloneable<CalibrateStatusItems>
    {
        [ObservableProperty]
        private string _calibrateItemName = string.Empty;

        [ObservableProperty]
        private bool _enabled;

        public CalibrateStatusItems Clone() => new()
        {
            CalibrateItemName = CalibrateItemName,
            Enabled = Enabled
        };

        public override bool Equals(object? obj)
        {
            return obj is CalibrateStatusItems items &&
                   CalibrateItemName == items.CalibrateItemName &&
                   Enabled == items.Enabled;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(CalibrateItemName, Enabled, CalibrateItemName, Enabled);
        }
    }
}
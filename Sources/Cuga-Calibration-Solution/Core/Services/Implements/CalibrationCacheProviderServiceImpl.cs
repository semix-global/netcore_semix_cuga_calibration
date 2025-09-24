using Core.Models.Models;
using Core.Models.Models.Ads.PressureGains;
using Core.Models.Models.Ads.XGains;
using Core.Models.Models.Ads.YGains;
using Core.Models.Models.Chuck.Center;
using Core.Models.Models.Chuck.Gantry;
using Core.Models.Models.Chuck.GlobalScaleError;
using Core.Models.Models.Chuck.Prealigner;
using Core.Models.Models.Chuck.RotateScaleError;
using Core.Models.Models.Chuck.StageMap;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Laser.AodDelay;
using Core.Models.Models.Laser.AutoFocus;
using Core.Models.Models.Laser.DOEAngle;
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
using Core.Utilities;
using Core.Wcf.Models;
using Core.Wcf.Models.Ads;
using Core.Wcf.Models.Chuck;
using Core.Wcf.Models.Laser;
using Core.Wcf.Models.Microscope;
using CugaCalibration.Core.Services.Interfaces;
using Local.NoSQL.DB.Providers.Extensions;
using Local.NoSQL.DB.Providers.Interfaces;
using Local.SQL.DB.Providers.Models.Entities.Base.Interface;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Models;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM.Providers;
using System.IO;

namespace CugaCalibration.Core.Services.Implements;

[IOCAppService(ServiceType = typeof(ICalibrationCacheProvider), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public class CalibrationCacheProviderServiceImpl(
    IOptions<ApplicationSetting> options,
    ICacheProvider cacheProvider,
    ILogger<CalibrationCacheProviderServiceImpl> logger,
    IDialogWindowProvider dialogWindowProvider,
    ApplicationCookie applicationCookie) : ICalibrationCacheProvider
{
    private readonly string _saveResultDirectory = Path.Combine(options.Value.AppHomeDirectory, "CalibrationResult");

    public async Task<bool> TrySaveAsync()
    {
        try
        {
            var tasks = new List<Task>();

            var calibrationObj = new CalibrationObj
            {
                CalibrationAdsObj = new CalibrationAdsObj(),
                CalibrationMicroscopeObj = new CalibrationMicroscopeObj(),
                CalibrationChuckObj = new CalibrationChuckObj(),
                CalibrationLaserObj = new CalibrationLaserObj()
            };

            tasks.Add(Task.Run(() => calibrationObj.CalibrationAdsObj.CalibrationAdsPressureGains = cacheProvider.GetOrDefault<AdsPressureGainsDto>().AdaptTo()));
            tasks.Add(Task.Run(() => calibrationObj.CalibrationAdsObj.CalibrationAdsXGains = cacheProvider.GetOrDefault<AdsXGainsItemDto>().AdaptTo()));
            tasks.Add(Task.Run(() => calibrationObj.CalibrationAdsObj.CalibrationAdsYGains = cacheProvider.GetOrDefault<AdsYGainsItemDto>().AdaptTo()));

            tasks.Add(Task.Run(() => calibrationObj.CalibrationMicroscopeObj.CalibrationMicroscopeFocusItemList = [.. cacheProvider.GetOrDefaultArray<MicroscopeFocusItemDto>().Select(t => t.AdaptTo())]));
            tasks.Add(Task.Run(() => calibrationObj.CalibrationMicroscopeObj.CalibrationMicroscopeCalChip = cacheProvider.GetOrDefault<MicroscopeCalChipDto>().AdaptTo()));
            tasks.Add(Task.Run(() => calibrationObj.CalibrationMicroscopeObj.CalibrationMicroscopePixelSizeItemList = [.. cacheProvider.GetOrDefaultArray<MicroscopePixelSizeItemDto>().Select(t => t.AdaptTo())]));
            tasks.Add(Task.Run(() => calibrationObj.CalibrationMicroscopeObj.CalibrationMicroscopeCentricityItemList = [.. cacheProvider.GetOrDefaultArray<MicroscopeCentricityItemDto>().Select(t => t.AdaptTo())]));

            tasks.Add(Task.Run(() => calibrationObj.CalibrationChuckObj.CalibrationChuckGantry = cacheProvider.GetOrDefault<ChuckGantryDto>().AdaptTo()));
            tasks.Add(Task.Run(() => calibrationObj.CalibrationChuckObj.CalibrationCenterObj = cacheProvider.GetOrDefault<ChuckCenterObjDto>().AdaptTo()));
            tasks.Add(Task.Run(() => calibrationObj.CalibrationChuckObj.CalibrationPrealignerObj = cacheProvider.GetOrDefault<ChuckPrealignerObjDto>().AdaptTo()));

            tasks.Add(Task.Run(() => calibrationObj.CalibrationChuckObj.CalibrationChuckStageMap = cacheProvider.GetOrDefault<ChuckStageMapDto>().AdaptTo()));
            tasks.Add(Task.Run(() => calibrationObj.CalibrationChuckObj.CalibrationChuckGlobalScaleError = cacheProvider.GetOrDefault<ChuckGlobalScaleErrorDto>().AdaptTo()));
            tasks.Add(Task.Run(() => calibrationObj.CalibrationChuckObj.CalibrationChuckRotateScaleError = cacheProvider.GetOrDefault<ChuckRotateScaleErrorDto>().AdaptTo()));

            tasks.Add(Task.Run(() => calibrationObj.CalibrationLaserObj.CalibrationLaserAutoFocus = cacheProvider.GetOrDefault<LaserAutoFocusDto>().AdaptTo()));
            tasks.Add(Task.Run(() => calibrationObj.CalibrationLaserObj.CalibrationLaserAodDelayItemList = [.. cacheProvider.GetOrDefaultArray<LaserAodDelayItemDto>().Select(t => t.AdaptTo())]));
            tasks.Add(Task.Run(() => calibrationObj.CalibrationLaserObj.CalibrationLaserXtcCalibrationItemList = [.. cacheProvider.GetOrDefaultArray<LaserXTCCalibrationItemDto>().Select(t => t.AdaptTo())]));
            tasks.Add(Task.Run(() => calibrationObj.CalibrationLaserObj.CalibrationLaserPixelSizeItemList = [.. cacheProvider.GetOrDefaultArray<LaserPixelSizeItemDto>().Select(t => t.AdaptTo())]));
            tasks.Add(Task.Run(() => calibrationObj.CalibrationLaserObj.CalibrationLaserXPixelSizeList = [.. cacheProvider.GetOrDefaultArray<LaserXPixelSizeItemDto>().Select(t => t.AdaptTo())]));
            tasks.Add(Task.Run(() => calibrationObj.CalibrationLaserObj.CalibrationLaserLineCentricityItemList = [.. cacheProvider.GetOrDefaultArray<LaserLineCentricityItemDto>().Select(t => t.AdaptTo())]));
            tasks.Add(Task.Run(() => calibrationObj.CalibrationLaserObj.CalibrationLaserIlluminationProfileItemList = [.. cacheProvider.GetOrDefaultArray<LaserIlluminationProfileItemDto>().Select(t => t.AdaptTo())]));
            tasks.Add(Task.Run(() => calibrationObj.CalibrationLaserObj.CalibrationLaserOpticalPowerList = [.. cacheProvider.GetOrDefaultArray<LaserOpticalPowerDto>().Select(t => t.AdaptTo())]));
            tasks.Add(Task.Run(() => calibrationObj.CalibrationLaserObj.CalibrationLaserXYAstigmatismItemList = [.. cacheProvider.GetOrDefaultArray<LaserXYAstigmatismCalibrationItemDto>().Select(t => t.AdaptTo())]));
            tasks.Add(Task.Run(() => calibrationObj.CalibrationLaserObj.CalibrationLaserDoeAngle = cacheProvider.GetOrDefault<LaserDOEAngleDto>().AdaptTo()));

            await Task.WhenAll(tasks).ConfigureAwait(false);

            FileHelper.SerializeOperate(calibrationObj, Path.Combine(_saveResultDirectory, $"Result_{DateTimeHelper.DateTime2String(DateTime.Now, Constants.LongFileDateTimeFormat)}.dat"));

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Save calibration result failed");
            return false;
        }
    }

    // todo:delete
    public bool TrySet<T>(T dto, CancellationToken cancellationToken) where T : class, ICacheItem, new()
    {
        return InvokeSave(update =>
        {
            update(dto);
            cacheProvider.Set(dto, cancellationToken);
            return true;
        }, typeof(T).Name);
    }

    // todo:delete
    public bool TrySetArray<T>(T[] dtoList, CancellationToken cancellationToken) where T : class, ICacheItem, new()
    {
        return InvokeSave(update =>
        {
            foreach (var dto in dtoList) update(dto);

            cacheProvider.SetArray(dtoList, cancellationToken);
            return true;
        }, typeof(T).Name);
    }

    public bool TrySetDisable<T>(CancellationToken cancellationToken) where T : CalibrationDtoBase, new()
    {
        var calibrationDtoBase = cacheProvider.GetOrDefault<T>();

        calibrationDtoBase.IsCalibrated = calibrationDtoBase.IsVerified = false;

        return TrySet(calibrationDtoBase, cancellationToken);
    }

    public bool TrySetArrayDisable<T>(CancellationToken cancellationToken) where T : CalibrationDtoBase, new()
    {
        var caches = cacheProvider.GetOrDefaultArray<T>();

        foreach (var calibrationDtoBase in caches)
        {
            calibrationDtoBase.IsCalibrated = calibrationDtoBase.IsVerified = false;
        }

        return TrySetArray(caches, cancellationToken);
    }

    public bool TrySetIsRequiredSelfCheck<T>(bool isRequiredSelfCheck, CancellationToken cancellationToken) where T : CalibrationDtoBase, new()
    {
        var calibrationDtoBase = cacheProvider.GetOrDefault<T>();

        if (calibrationDtoBase.IsRequiredSelfCheck == isRequiredSelfCheck) // 避免重复写入
            return true;

        calibrationDtoBase.IsRequiredSelfCheck = isRequiredSelfCheck;

        return TrySet(calibrationDtoBase, cancellationToken);
    }

    public bool TrySetArrayIsRequiredSelfCheck<T>(bool isRequiredSelfCheck, CancellationToken cancellationToken) where T : CalibrationDtoBase, new()
    {
        var caches = cacheProvider.GetOrDefaultArray<T>();

        if (caches.All(t => t.IsRequiredSelfCheck == isRequiredSelfCheck)) // 避免重复写入
            return true;

        foreach (var calibrationDtoBase in caches)
        {
            calibrationDtoBase.IsRequiredSelfCheck = isRequiredSelfCheck;
        }

        return TrySetArray(caches, cancellationToken);
    }

    private bool InvokeSave(Func<Action<ICacheItem>, bool> func, string name)
    {
        while (true)
        {
            if (func(Update)) return true;

            dialogWindowProvider.TryShowDialog($"Save {name} Failed!", out var dialogResultEnum, DialogButtonsEnum.RetryCancel, DialogIconEnum.Warning);

            if (dialogResultEnum == DialogResultEnum.Retry) continue;

            return false;
        }

        void Update(ICacheItem cacheItem)
        {
            cacheItem.Id = 0;
            cacheItem.CreatedTime = DateTime.Now;
            cacheItem.IsDeleted = false;

            if (cacheItem is not IEntityAdd entity) return;

            entity.CreatedUserId = applicationCookie.SysUser.Id;
            entity.CreatedUserName = applicationCookie.SysUser.UserName;
        }
    }
}
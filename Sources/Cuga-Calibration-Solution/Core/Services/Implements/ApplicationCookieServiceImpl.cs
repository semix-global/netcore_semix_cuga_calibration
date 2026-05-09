using CommunityToolkit.Diagnostics;
using Core.Models.Helper;
using Core.Models.Models;
using Core.Models.Models.CIB.LineCentricity;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Common.Pattern;
using Core.Services.Interfaces;
using Core.Utilities;
using CugaCalibration.Core.Services.Interfaces;
using CugaCalibration.ViewModels;
using Local.SQL.Cache.Providers.Services.Interfaces;
using Local.SQL.DB.Providers.Models.Entities.DTO;
using Local.SQL.DB.Providers.Models.Enums;
using Local.SQL.DB.Providers.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Helpers;
using Net.Utilities.Models;
using Net.Utilities.Models.Geometries;
using Net.Utilities.WPF.MVVM;

namespace CugaCalibration.Core.Services.Implements;

[IOCAppService(ServiceType = typeof(IApplicationCookieService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed class ApplicationCookieServiceImpl(
    ICalibrationStageService calibrationStageServiceImpl,
    [FromKeyedServices(CalibrationConstantsHelper.RecipeDbKey)]
    ICacheProvider cacheProvider,
    ApplicationCookie applicationCookie,
    IOptions<ApplicationSetting> options) : IApplicationCookieService
{
    private const int TimeoutSecond = 60;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    public async Task LoadingSystemMenuCookieAsync(SysUserDTO sysUserDto, CancellationToken cancellationToken)
    {
        var sysMenuService = HostApplication.GetRequiredService<ISysMenuService>();
        applicationCookie.SysUser.Update(sysUserDto);
        applicationCookie.AllRoleSysMenus = await sysMenuService.GetAllAsync(cancellationToken).ConfigureAwait(false);
        applicationCookie.CurrentRoleSysMenus = sysUserDto.IsAdmin
            ? [.. applicationCookie.AllRoleSysMenus]
            : [.. sysUserDto.SysRoleList.SelectMany(t => t.SysMenuList).DistinctBy(t => t.Id)];

        applicationCookie.CalibrationMenu = BuildCalibrationMenuTree();
        applicationCookie.TitleMenu = BuildTitleMenuTree();
        UpdateCalibrationMenuEntry(applicationCookie.CalibrationMenu);

        return;

        CalibrationMenu BuildCalibrationMenuTree()
        {
            var calibrationMenus = applicationCookie.CurrentRoleSysMenus.Select(t => new CalibrationMenu { SysMenu = t }).ToArray();
            var baseCalibrationMenu = calibrationMenus.Single(t => t.SysMenu.Name == options.Value.CalibrationMenuName && t.SysMenu.MenuTypeEnum == MenuTypeEnum.Catalog);

            RecursionFn(baseCalibrationMenu);

            return baseCalibrationMenu;

            void RecursionFn(CalibrationMenu calibrationItem)
            {
                var children = calibrationMenus
                    .Where(p => p.SysMenu.ParentId == calibrationItem.SysMenu.Id && p.SysMenu.MenuTypeEnum is MenuTypeEnum.Catalog or MenuTypeEnum.Menu)
                    .OrderBy(t => t.SysMenu.OrderNum)
                    .ToArray();
                calibrationItem.Children = children;

                foreach (var item in children
                             .Where(t => calibrationMenus.Any(p => p.SysMenu.ParentId == t.SysMenu.Id && p.SysMenu.MenuTypeEnum is MenuTypeEnum.Catalog or MenuTypeEnum.Menu)))
                {
                    RecursionFn(item);
                }
            }
        }

        SysMenuDTO BuildTitleMenuTree()
        {
            var baseSysMenu = applicationCookie.CurrentRoleSysMenus.Single(t => t.Name == options.Value.TitleMenuName && t.MenuTypeEnum == MenuTypeEnum.Catalog);
            RecursionFn(baseSysMenu);

            return baseSysMenu;

            void RecursionFn(SysMenuDTO calibrationItem)
            {
                var children = applicationCookie.CurrentRoleSysMenus
                    .Where(p => p.ParentId == calibrationItem.Id && p.MenuTypeEnum is MenuTypeEnum.Catalog or MenuTypeEnum.Menu)
                    .OrderBy(t => t.OrderNum)
                    .ToList();
                calibrationItem.ChildList = children;

                foreach (var item in children.Where(item => applicationCookie.CurrentRoleSysMenus.Any(p => p.ParentId == item.Id && p.MenuTypeEnum is MenuTypeEnum.Catalog or MenuTypeEnum.Menu)))
                {
                    RecursionFn(item);
                }
            }
        }

        static void UpdateCalibrationMenuEntry(CalibrationMenu calibrationMenu)
        {
            if (calibrationMenu.SysMenu.MenuTypeEnum == MenuTypeEnum.Menu)
            {
                var type = Guard.IsNotNullAndReturn(Type.GetType(calibrationMenu.SysMenu.Component), $"Code bug: Cannot find type by component {calibrationMenu.SysMenu.Component} for calibration menu {calibrationMenu.SysMenu.Name}.");

                Guard.IsTrue(ApplicationCookie.CalibrationViewModelEntries.ContainsKey(type), $"Code bug: CalibrationViewModelEntries does not contain key {type.FullName} for calibration menu {calibrationMenu.SysMenu.Name}.");

                calibrationMenu.Entry = ApplicationCookie.CalibrationViewModelEntries[type];
            }

            foreach (var child in calibrationMenu.Children) UpdateCalibrationMenuEntry(child);
        }
    }

    public IReadOnlyList<SysMenuDTO> FindSysMenusByRecursionSysMenuComponent(string component)
    {
        var result = new List<SysMenuDTO>();

        var sysMenuDto = applicationCookie.AllRoleSysMenus.SingleOrDefault(t => t.Component == component);
        if (sysMenuDto is not null) RecursionFn([sysMenuDto]);

        return result;

        void RecursionFn(IReadOnlyList<SysMenuDTO> sysMenus)
        {
            var children = applicationCookie.AllRoleSysMenus.Where(p => sysMenus.Any(t => p.ParentId == t.Id)).ToArray();
            result.AddRange(children);

            if (sysMenus.Count == 0) return;

            RecursionFn(children);
        }
    }

    public IReadOnlyCollection<(int Pmt, Point Offset)> GetLineCentricityMachineOffsetList(IReadOnlyCollection<CIBLineCentricityDTO> result, ProductivityInformation productivityInformation)
    {
        var (xDirection, yDirection) = calibrationStageServiceImpl.GetMachineDirection().Anything;

        var cache = Guard.IsNotNullAndReturn(cacheProvider.GetOrDefault<CIBLineCentricityCache>());

        var resultList = result.Where(t => t.ProductivityInformation == productivityInformation)
            .OrderBy(t => t.PmtId)
            .ToList();

        var centerItemDto = resultList.Single(t => t.PmtId == CalibrationConstantsHelper.MainPmtId);

        var offsetList = resultList.OrderBy(t => t.PmtId)
            .Select(t =>
            {
                var centerOffset = t.DFMachineCenterPosition - (Vector)centerItemDto.DFMachineCenterPosition;
                return (t.PmtId, new Point(xDirection * centerOffset.X, yDirection * centerOffset.Y) - (Vector)new Point(0, cache.PmtInterval * (t.PmtId - CalibrationConstantsHelper.MainPmtId)));
            })
            .ToList();

        return offsetList;
    }

    #region Cache

    public CalibrationCacheBase GetCache(Type cacheType, CancellationToken cancellationToken = default) => Invoke(() =>
    {
        var entry = ApplicationCookie.CalibrationViewModelEntries.Values.Single(e => e.CacheType == cacheType);

        if (Guard.IsAssignableToTypeAndReturn<CalibrationCacheBase?>(entry.Cookie.Cache) is not null) return entry.Cookie.Cache;

        if (cacheProvider.TryGetOrDefault(entry.CacheType, out var cache) == false)
        {
            cache = Activator.CreateInstance(entry.CacheType);
            cacheProvider.Set(entry.CacheType, cache, cancellationToken);
        }

        ObjectHelper.SetPropertyValue(entry.Cookie, nameof(entry.Cookie.Cache), cache);

        return (CalibrationCacheBase)cache;
    }, cancellationToken);

    public T GetCache<T>(CancellationToken cancellationToken = default) where T : CalibrationCacheBase, new() => (T)GetCache(typeof(T), cancellationToken);

    public void SetCache(Type cacheType, CalibrationCacheBase value, CancellationToken cancellationToken = default) => Invoke(() =>
    {
        var entry = ApplicationCookie.CalibrationViewModelEntries.Values.Single(e => e.CacheType == cacheType);

        cacheProvider.Set(cacheType, value, cancellationToken);
        ObjectHelper.SetPropertyValue(entry.Cookie, nameof(entry.Cookie.Cache), value);

        return Unit.Default;
    }, cancellationToken);

    public void SetCache<T>(T value, CancellationToken cancellationToken = default) where T : CalibrationCacheBase, new() => SetCache(typeof(T), value, cancellationToken);

    #endregion

    #region Calibration

    public CalibrationDTOBase GetCalibration(Type dtoType, CancellationToken cancellationToken = default) => Invoke(() =>
    {
        var entry = ApplicationCookie.CalibrationViewModelEntries.Values.Single(e => e.DTOType == dtoType && e.IsArray == false);

        if (Guard.IsAssignableToTypeAndReturn<CalibrationDTOBase?>(entry.Cookie.Calibration) is not null) return entry.Cookie.Calibration;

        var calibrationViewModel = Guard.IsAssignableToTypeAndReturn<CalibrationViewModelBase>(HostApplication.GetRequiredService(entry.ViewModelType));

        if (cacheProvider.TryGetOrDefault(entry.DTOType, out var calibration) == false)
        {
            calibration = Activator.CreateInstance(entry.DTOType);
            cacheProvider.Set(entry.DTOType, calibration, cancellationToken);
        }

        ObjectHelper.SetPropertyValue(entry.Cookie, nameof(entry.Cookie.Calibration), calibration);

        return (CalibrationDTOBase)calibration;
    }, cancellationToken);

    public T GetCalibration<T>(CancellationToken cancellationToken = default) where T : CalibrationDTOBase, new() => (T)GetCalibration(typeof(T), cancellationToken);

    public CalibrationDTOBase[] GetCalibrations(Type dtoType, CancellationToken cancellationToken = default) => Invoke(() =>
    {
        var entry = ApplicationCookie.CalibrationViewModelEntries.Values.Single(e => e.DTOType == dtoType && e.IsArray);

        if (Guard.IsAssignableToTypeAndReturn<CalibrationDTOBase[]?>(entry.Cookie.Calibrations) is not null) return entry.Cookie.Calibrations;

        var calibrationViewModel = Guard.IsAssignableToTypeAndReturn<CalibrationViewModelBase>(HostApplication.GetRequiredService(entry.ViewModelType));

        if (cacheProvider.TryGetOrDefaultArray(entry.DTOType, out var calibrations) == false)
            cacheProvider.SetArray(entry.DTOType, calibrations, cancellationToken);

        ObjectHelper.SetPropertyValue(entry.Cookie, nameof(entry.Cookie.Calibrations), calibrations);

        return (CalibrationDTOBase[])calibrations;
    }, cancellationToken);

    public T[] GetCalibrations<T>(CancellationToken cancellationToken = default) where T : CalibrationDTOBase, new() => (T[])GetCalibrations(typeof(T), cancellationToken);

    public void SetCalibration(Type dtoType, CalibrationDTOBase value, CancellationToken cancellationToken = default) => Invoke(() =>
    {
        var entry = ApplicationCookie.CalibrationViewModelEntries.Values.Single(e => e.DTOType == dtoType && e.IsArray == false);

        cacheProvider.Set(dtoType, value, cancellationToken);
        ObjectHelper.SetPropertyValue(entry.Cookie, nameof(entry.Cookie.Calibration), value);

        return Unit.Default;
    }, cancellationToken);

    public void SetCalibration<T>(T value, CancellationToken cancellationToken = default) where T : CalibrationDTOBase, new() => SetCalibration(typeof(T), value, cancellationToken);

    public void SetCalibrations(Type dtoType, CalibrationDTOBase[] value, CancellationToken cancellationToken = default) => Invoke(() =>
    {
        var entry = ApplicationCookie.CalibrationViewModelEntries.Values.Single(e => e.DTOType == dtoType && e.IsArray);

        cacheProvider.SetArray(dtoType, value.Cast<object>().ToArray(), cancellationToken);
        ObjectHelper.SetPropertyValue(entry.Cookie, nameof(entry.Cookie.Calibrations), value);

        return Unit.Default;
    }, cancellationToken);

    public void SetCalibrations<T>(T[] value, CancellationToken cancellationToken = default) where T : CalibrationDTOBase, new() => SetCalibrations(typeof(T), value, cancellationToken);

    #endregion

    private T Invoke<T>(Func<T> func, CancellationToken cancellationToken)
    {
        var isRelease = false;
        try
        {
            isRelease = _semaphore.Wait(TimeSpan.FromSeconds(TimeoutSecond), cancellationToken);

            return isRelease ? func() : ThrowHelper.ThrowTimeoutException<T>();
        }
        finally
        {
            if (isRelease) _semaphore.Release();
        }
    }
}
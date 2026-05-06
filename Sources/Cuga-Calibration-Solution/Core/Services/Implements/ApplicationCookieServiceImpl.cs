using CommunityToolkit.Diagnostics;
using Core.Models.Helper;
using Core.Models.Models.CIB.LineCentricity;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Common.Pattern;
using Core.Services.Interfaces;
using Core.Utilities;
using CugaCalibration.Core.Services.Interfaces;
using Local.SQL.Cache.Providers.Services.Interfaces;
using Local.SQL.DB.Providers.Models.Entities.DTO;
using Local.SQL.DB.Providers.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models.Geometries;
using Net.Utilities.WPF.MVVM;
using static Local.SQL.DB.Providers.Models.Enums.MenuTypeEnum;

namespace CugaCalibration.Core.Services.Implements;

[IOCAppService(ServiceType = typeof(IApplicationCookieService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed class ApplicationCookieServiceImpl(
    ICalibrationStageService calibrationStageServiceImpl,
    [FromKeyedServices(CalibrationConstantsHelper.RecipeDbKey)]
    ICacheProvider cacheProvider,
    ApplicationCookie applicationCookie,
    IOptions<ApplicationSetting> options) : IApplicationCookieService
{
    public async Task UpdateCookieAsync(SysUserDTO sysUserDto, CancellationToken cancellationToken)
    {
        var sysMenuService = HostApplication.GetRequiredService<ISysMenuService>();
        applicationCookie.SysUser.Update(sysUserDto);
        applicationCookie.AllRoleSysMenus = await sysMenuService.GetAllAsync(cancellationToken).ConfigureAwait(false);
        applicationCookie.CurrentRoleSysMenus = sysUserDto.IsAdmin
            ? [.. applicationCookie.AllRoleSysMenus]
            : [.. sysUserDto.SysRoleList.SelectMany(t => t.SysMenuList).DistinctBy(t => t.Id)];

        UpdateCalibrationMenu();
        UpdateTitleMenu();
        return;

        void UpdateCalibrationMenu()
        {
            var calibrationItems = applicationCookie.CurrentRoleSysMenus.Select(t => new CalibrationMenu { SysMenu = t }).ToList();
            var buildMenuTree = BuildMenuTree(calibrationItems);
            if (buildMenuTree is null) return;

            applicationCookie.CalibrationMenu = buildMenuTree;

            return;

            CalibrationMenu? BuildMenuTree(List<CalibrationMenu> menus)
            {
                var calibrationItem = menus.SingleOrDefault(t => t.SysMenu.Name == options.Value.CalibrationMenuName && t.SysMenu.MenuTypeEnum == Catalog);
                if (calibrationItem is null) return null;

                RecursionFn(menus, calibrationItem);

                return calibrationItem;

                static void RecursionFn(List<CalibrationMenu> list, CalibrationMenu calibrationItem)
                {
                    // 得到子节点列表
                    var childList = list.Where(p => p.SysMenu.ParentId == calibrationItem.SysMenu.Id && p.SysMenu.MenuTypeEnum is Catalog or Menu).ToList();
                    calibrationItem.Children = [.. childList.OrderBy(t => t.SysMenu.OrderNum)];

                    foreach (var item in childList.Where(item => list.Any(p => p.SysMenu.ParentId == item.SysMenu.Id && p.SysMenu.MenuTypeEnum is Catalog or Menu)))
                    {
                        RecursionFn(list, item);
                    }
                }
            }
        }

        void UpdateTitleMenu()
        {
            var buildMenuTree = BuildMenuTree(applicationCookie.CurrentRoleSysMenus);
            if (buildMenuTree is null) return;

            applicationCookie.TitleMenu = buildMenuTree;

            return;

            SysMenuDTO? BuildMenuTree(IReadOnlyList<SysMenuDTO> menus)
            {
                var sysMenuDto = menus.SingleOrDefault(t => t.Name == options.Value.TitleMenuName && t.MenuTypeEnum == Catalog);
                if (sysMenuDto is null) return null;

                RecursionFn(menus, sysMenuDto);

                return sysMenuDto;

                static void RecursionFn(IReadOnlyList<SysMenuDTO> list, SysMenuDTO calibrationItem)
                {
                    // 得到子节点列表
                    var childList = list.Where(p => p.ParentId == calibrationItem.Id && p.MenuTypeEnum is Catalog or Menu).ToList();
                    calibrationItem.ChildList = [.. childList.OrderBy(t => t.OrderNum)];

                    foreach (var item in childList.Where(item => list.Any(p => p.ParentId == item.Id && p.MenuTypeEnum is Catalog or Menu)))
                    {
                        RecursionFn(list, item);
                    }
                }
            }
        }
    }

    public CalibrationMenu? FindCalibrationItem<TViewModel>()
    {
        return Find(applicationCookie.CalibrationMenu.Children);

        static CalibrationMenu? Find(IReadOnlyList<CalibrationMenu> items)
        {
            foreach (var item in items)
            {
                if (item.SysMenu.Component == typeof(TViewModel).FullName) return item;

                var foundItem = Find(item.Children);
                if (foundItem is not null) return foundItem;
            }

            return null;
        }
    }

    public List<SysMenuDTO> FindSysMenuListByRecursionComponent(string component)
    {
        var result = new List<SysMenuDTO>();

        var sysMenuDto = applicationCookie.AllRoleSysMenus.SingleOrDefault(t => t.Component == component);
        if (sysMenuDto is not null) RecursionFn([sysMenuDto]);

        return result;

        void RecursionFn(List<SysMenuDTO> list)
        {
            var childList = applicationCookie.AllRoleSysMenus.Where(p => list.Any(t => p.ParentId == t.Id)).ToList();
            result.AddRange(childList);

            if (list.Count == 0) return;
            RecursionFn(childList);
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
}
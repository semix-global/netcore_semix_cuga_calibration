using CommunityToolkit.Diagnostics;
using Core.Models.Helper;
using Core.Models.Models.CIB.LineCentricity;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Common.Pattern;
using Core.Services.Interfaces;
using Core.Utilities;
using CugaCalibration.Core.Services.Interfaces;
using Local.SQL.Cache.Providers.Interfaces;
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
        applicationCookie.AllRoleSysMenuList = await sysMenuService.GetAllAsync(cancellationToken).ConfigureAwait(false);
        applicationCookie.RoleSysMenuList = sysUserDto.IsAdmin
            ? [.. applicationCookie.AllRoleSysMenuList]
            : [.. sysUserDto.SysRoleList.SelectMany(t => t.SysMenuList).DistinctBy(t => t.Id)];

        UpdateCalibrationMenu();
        UpdateTitleMenu();
        return;

        void UpdateCalibrationMenu()
        {
            var calibrationItems = applicationCookie.RoleSysMenuList.Select(t => new CalibrationMenu { SysMenuDTO = t }).ToList();
            var buildMenuTree = BuildMenuTree(calibrationItems);
            if (buildMenuTree is null) return;

            applicationCookie.CalibrationMenu = buildMenuTree;

            return;

            CalibrationMenu? BuildMenuTree(List<CalibrationMenu> menus)
            {
                var calibrationItem = menus.SingleOrDefault(t => t.SysMenuDTO.Name == options.Value.CalibrationMenuName && t.SysMenuDTO.MenuTypeEnum == Catalog);
                if (calibrationItem is null) return null;

                RecursionFn(menus, calibrationItem);

                return calibrationItem;

                static void RecursionFn(List<CalibrationMenu> list, CalibrationMenu calibrationItem)
                {
                    // 得到子节点列表
                    var childList = list.Where(p => p.SysMenuDTO.ParentId == calibrationItem.SysMenuDTO.Id && p.SysMenuDTO.MenuTypeEnum is Catalog or Menu).ToList();
                    calibrationItem.ChildList = [.. childList.OrderBy(t => t.SysMenuDTO.OrderNum)];

                    foreach (var item in childList.Where(item => list.Any(p => p.SysMenuDTO.ParentId == item.SysMenuDTO.Id && p.SysMenuDTO.MenuTypeEnum is Catalog or Menu)))
                    {
                        RecursionFn(list, item);
                    }
                }
            }
        }

        void UpdateTitleMenu()
        {
            var buildMenuTree = BuildMenuTree(applicationCookie.RoleSysMenuList);
            if (buildMenuTree is null) return;

            applicationCookie.TitleMenu = buildMenuTree;

            return;

            SysMenuDTO? BuildMenuTree(List<SysMenuDTO> menus)
            {
                var sysMenuDto = menus.SingleOrDefault(t => t.Name == options.Value.TitleMenuName && t.MenuTypeEnum == Catalog);
                if (sysMenuDto is null) return null;

                RecursionFn(menus, sysMenuDto);

                return sysMenuDto;

                static void RecursionFn(List<SysMenuDTO> list, SysMenuDTO calibrationItem)
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
        return Find(applicationCookie.CalibrationMenu.ChildList);

        static CalibrationMenu? Find(IList<CalibrationMenu> items)
        {
            foreach (var item in items)
            {
                if (item.SysMenuDTO.Component == typeof(TViewModel).FullName) return item;

                var foundItem = Find(item.ChildList);
                if (foundItem is not null) return foundItem;
            }

            return null;
        }
    }

    public List<SysMenuDTO> FindSysMenuListByRecursionComponent(string component)
    {
        var result = new List<SysMenuDTO>();

        var sysMenuDto = applicationCookie.AllRoleSysMenuList.SingleOrDefault(t => t.Component == component);
        if (sysMenuDto is not null) RecursionFn([sysMenuDto]);

        return result;

        void RecursionFn(List<SysMenuDTO> list)
        {
            var childList = applicationCookie.AllRoleSysMenuList.Where(p => list.Any(t => p.ParentId == t.Id)).ToList();
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
using CommunityToolkit.Diagnostics;
using Core.Models;
using Core.Models.Helper;
using Core.Models.Models.CIB.LineCentricity;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Setting;
using Core.Services.Interfaces;
using CugaCalibration.Core.Services.Interfaces;
using Local.SQL.Cache.Providers.Services.Interfaces;
using Local.SQL.DB.Providers.Models.Entities.DTO;
using Local.SQL.DB.Providers.Models.Enums;
using Local.SQL.DB.Providers.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models.Geometries;
using Net.Utilities.WPF.MVVM;

namespace CugaCalibration.Core.Services.Implements;

[IOCAppService(ServiceType = typeof(IApplicationCookieService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed partial class ApplicationCookieServiceImpl(
    ICalibrationStageService calibrationStageServiceImpl,
    ICacheProvider cacheProvider,
    [FromKeyedServices(CalibrationConstantsHelper.RecipeDbKey)]
    ICacheProvider recipeCacheProvider,
    ApplicationCookie applicationCookie,
    IOptions<ApplicationSetting> options,
    ILogger<ApplicationCookieServiceImpl> logger,
    CalibrationSetting calibrationSetting) : IApplicationCookieService
{
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

                foreach (var child in children) child.SysMenu.Parent = calibrationItem.SysMenu;

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

                foreach (var child in children) child.Parent = calibrationItem;

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

        var resultList = result.Where(t => t.ProductivityInformation == productivityInformation)
            .OrderBy(t => t.PmtId)
            .ToList();

        var centerItemDto = resultList.Single(t => t.PmtId == CalibrationConstantsHelper.MainPmtId);

        var offsetList = resultList.OrderBy(t => t.PmtId)
            .Select(t =>
            {
                var centerOffset = t.DFMachineCenterPosition - (Vector)centerItemDto.DFMachineCenterPosition;
                return (t.PmtId, new Point(xDirection * centerOffset.X, yDirection * centerOffset.Y) - (Vector)new Point(0, calibrationSetting.SettingCommonParam.PMTInterval * (t.PmtId - CalibrationConstantsHelper.MainPmtId)));
            })
            .ToList();

        return offsetList;
    }
}
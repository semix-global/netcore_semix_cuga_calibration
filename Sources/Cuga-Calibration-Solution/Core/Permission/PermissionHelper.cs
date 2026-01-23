using Core.Models.Models.Common.Cookies;
using CugaCalibration.Core.Services.Interfaces;
using Local.SQL.DB.Providers.Models.Enums;
using Net.Utilities.Helpers.Helpers;
using Net.Utilities.WPF.Helper;
using Net.Utilities.WPF.MVVM;
using System.Windows;
using System.Windows.Controls;

namespace CugaCalibration.Core.Permission;

/// <summary>
/// 权限控制辅助类
/// 提供静态方法用于应用 UI 权限控制
/// </summary>
public static class PermissionHelper
{
    /// <summary>
    /// 应用权限控制到指定的 FrameworkElement
    /// </summary>
    /// <param name="frameworkElement">目标元素</param>
    public static void ApplyPermissions(FrameworkElement frameworkElement)
    {
        if (frameworkElement.DataContext is null) return;

        var applicationCookie = HostApplication.GetRequiredService<ApplicationCookie>();
        var applicationCookieService = HostApplication.GetRequiredService<IApplicationCookieService>();

        // 管理员跳过权限检查
        if (applicationCookie.SysUser.IsAdmin) return;

        // 根据 ViewModel 类型名查找关联的菜单权限配置
        var componentName = frameworkElement.DataContext.GetType().FullName!;
        var menuList = applicationCookieService.FindSysMenuListByRecursionComponent(componentName);

        foreach (var menuDetail in menuList.Where(t =>
                     !string.IsNullOrWhiteSpace(t.Component) &&
                     !string.IsNullOrWhiteSpace(t.Perms)))
        {
            // 如果用户有该权限则跳过
            if (applicationCookie.RoleSysMenuList.Any(t => t.Id == menuDetail.Id))
                continue;

            ApplyPermissionToControl(frameworkElement, menuDetail.Component!, menuDetail.Perms!, menuDetail.MenuTypeEnum);
        }
    }

    /// <summary>
    /// 将权限控制应用到具体控件
    /// </summary>
    private static void ApplyPermissionToControl(
        FrameworkElement root,
        string componentTypeName,
        string controlName,
        MenuTypeEnum menuType)
    {
        var componentType = Type.GetType(componentTypeName);
        if (componentType is null) return;

        foreach (var descendant in DependencyObjectHelper
                     .FindVisualDescendants(root, componentType)
                     .OfType<FrameworkElement>())
        {
            var control = descendant.FindName(controlName);
            if (control is null) continue;

            ApplyPermissionType(control, menuType);
        }
    }

    /// <summary>
    /// 根据权限类型设置控件属性
    /// </summary>
    private static void ApplyPermissionType(object control, MenuTypeEnum menuType)
    {
        switch (menuType)
        {
            case MenuTypeEnum.Readable:
                SetPropertyIfValid<bool>(control, nameof(TextBox.IsReadOnly), true);
                break;

            case MenuTypeEnum.Visible:
                SetPropertyIfValid<Visibility>(control, nameof(UIElement.Visibility), Visibility.Collapsed);
                break;

            case MenuTypeEnum.Enable:
                SetPropertyIfValid<bool>(control, nameof(UIElement.IsEnabled), false);
                break;
        }
    }

    /// <summary>
    /// 安全地设置属性值
    /// </summary>
    private static void SetPropertyIfValid<T>(object target, string propertyName, T value)
    {
        var propertyValue = ObjectHelper.GetPropertyValue(target, propertyName);
        if (propertyValue is not T) return;

        ObjectHelper.SetPropertyValue(target, propertyName, value!);
    }
}
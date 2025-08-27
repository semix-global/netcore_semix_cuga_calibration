using ArxOne.MrAdvice.Advice;
using Core.Models.Models.Common.Cookies;
using CugaCalibration.Core.Services.Interfaces;
using Local.SQL.DB.Providers.Models.Enums;
using Net.Utilities.Helpers.Helpers;
using Net.Utilities.WPF.Helper;
using Net.Utilities.WPF.MVVM;
using System.Windows;
using System.Windows.Controls;

namespace CugaCalibration.Core.Attribute;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor)]
public sealed class PermissionAttribute : System.Attribute, IMethodAdvice
{
    public void Advise(MethodAdviceContext context)
    {
        context.Proceed();
        OnExit(context);
    }

    private static void OnExit(MethodAdviceContext context)
    {
        if (context.Target is not FrameworkElement frameworkElement) return;
        if (frameworkElement.DataContext is null) return;

        var applicationCookie = HostApplication.GetRequiredService<ApplicationCookie>();
        var applicationCookieService = HostApplication.GetRequiredService<IApplicationCookieService>();

        if (applicationCookie.SysUser.IsAdmin) return;

        var list = applicationCookieService.FindSysMenuListByRecursionComponent(frameworkElement.DataContext.GetType().FullName!);
        foreach (var detail in list.Where(t => string.IsNullOrWhiteSpace(t.Component) == false && string.IsNullOrWhiteSpace(t.Perms) == false))
        {
            // 有权限的不处理
            if (applicationCookie.RoleSysMenuList.Any(t => t.Id == detail.Id)) continue;

            var type = Type.GetType(detail.Component);
            if (type is null) continue;

            foreach (var findVisualDescendant in DependencyObjectHelper.FindVisualDescendants(frameworkElement, type).OfType<FrameworkElement>())
            {
                var findName = findVisualDescendant!.FindName(detail.Perms!);
                if (findName is null) continue;

                switch (detail.MenuTypeEnum)
                {
                    case MenuTypeEnum.Readable:
                        var propertyValue = ObjectHelper.GetPropertyValue(findName, nameof(TextBox.IsReadOnly));
                        if (propertyValue is not bool) continue;

                        ObjectHelper.SetPropertyValue(findName, nameof(TextBox.IsReadOnly), true);

                        break;

                    case MenuTypeEnum.Visible:
                        var visibility = ObjectHelper.GetPropertyValue(findName, nameof(UIElement.Visibility));
                        if (visibility is not Visibility) continue;

                        ObjectHelper.SetPropertyValue(findName, nameof(UIElement.Visibility), Visibility.Collapsed);

                        break;

                    case MenuTypeEnum.Enable:
                        var isEnabled = ObjectHelper.GetPropertyValue(findName, nameof(UIElement.IsEnabled));
                        if (isEnabled is not bool) continue;

                        ObjectHelper.SetPropertyValue(findName, nameof(UIElement.IsEnabled), false);

                        break;
                }
            }
        }
    }
}
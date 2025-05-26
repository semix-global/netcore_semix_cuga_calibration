using System.Windows;
using System.Windows.Controls;

namespace Net.Utilities.WPF.Helper;

public static class PageHelper
{
    /// <summary>
    /// 确保视图是一个页面或提供一个页面
    /// </summary>
    /// <param name="view">View</param>
    /// <returns>Page</returns>
    public static Page EnsurePage(UIElement view)
    {
        if (view is Page page)
        {
            return page;
        }

        page = new Page { Content = view };

        return page;
    }
}
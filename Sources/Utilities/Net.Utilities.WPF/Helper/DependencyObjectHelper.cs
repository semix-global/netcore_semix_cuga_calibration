using System.Windows;
using System.Windows.Media;

namespace Net.Utilities.WPF.Helper;

public static class DependencyObjectHelper
{
    #region Child

    /// <summary>
    /// 获取element的指定类型的后代为Type第一个元素
    /// </summary>
    /// <param name="parent">父元素</param>
    /// <typeparam name="T">类型</typeparam>
    /// <returns>为Type第一个元素</returns>
    public static T? FindVisualDescendant<T>(DependencyObject parent) where T : DependencyObject => FindVisualDescendant(parent, typeof(T)) as T;

    /// <summary>
    /// 获取element的指定类型的后代为Type所有元素
    /// </summary>
    /// <param name="parent">父元素</param>
    /// <typeparam name="T">类型</typeparam>
    /// <returns>为Type所有元素</returns>
    public static IEnumerable<T> FindVisualDescendants<T>(DependencyObject parent) where T : DependencyObject => FindVisualDescendants(parent, typeof(T)).Cast<T>();

    /// <summary>
    /// 获取element的指定类型的后代为Type第一个元素
    /// </summary>
    /// <param name="parent">父元素</param>
    /// <param name="type">类型</param>
    /// <returns>为Type第一个元素</returns>
    public static DependencyObject? FindVisualDescendant(DependencyObject parent, Type type)
    {
        switch (parent)
        {
            case null:
                return null;

            case { } result when result.GetType() == type:
                return result;
        }

        (parent as FrameworkElement)?.ApplyTemplate();

        DependencyObject? foundElement = null;
        var childrenCount = VisualTreeHelper.GetChildrenCount(parent);
        for (var i = 0; i < childrenCount; i++)
        {
            var visual = VisualTreeHelper.GetChild(parent, i);
            foundElement = FindVisualDescendant(visual, type);
            if (foundElement is not null) break;
        }

        return foundElement;
    }

    /// <summary>
    /// 获取element的指定类型的后代为Type所有元素
    /// </summary>
    /// <param name="parent">父元素</param>
    /// <param name="type">类型</param>
    /// <returns>为Type所有元素</returns>
    public static IEnumerable<DependencyObject> FindVisualDescendants(DependencyObject parent, Type type)
    {
        switch (parent)
        {
            case null:
                yield break;

            case { } result when result.GetType() == type:
                yield return result;
                break;
        }

        (parent as FrameworkElement)?.ApplyTemplate();

        var childrenCount = VisualTreeHelper.GetChildrenCount(parent);
        for (var i = 0; i < childrenCount; i++)
        {
            var visual = VisualTreeHelper.GetChild(parent, i);
            foreach (var descendant in FindVisualDescendants(visual, type))
            {
                yield return descendant;
            }
        }
    }

    /// <summary>
    /// 判断child是否为parent的子元素
    /// </summary>
    /// <param name="child">子</param>
    /// <param name="parent">父</param>
    /// <returns>是不是子元素</returns>
    public static bool IsChildOf(DependencyObject child, DependencyObject parent)
    {
        var temp = child;

        while (temp is not null && temp != parent)
        {
            temp = VisualTreeHelper.GetParent(temp);
        }

        return temp == parent;
    }

    #endregion Child

    #region Parrent

    /// <summary>
    /// 获取element的指定类型的前代为Type的第一个元素
    /// </summary>
    /// <param name="child">子元素</param>
    /// <typeparam name="T">类型</typeparam>
    /// <returns>前代为Type的第一个元素</returns>
    public static T? FindVisualAncestor<T>(DependencyObject child) where T : DependencyObject => FindVisualAncestor(child, typeof(T)) as T;

    /// <summary>
    /// 获取element的指定类型的前代为Type的元素
    /// </summary>
    /// <param name="child">子元素</param>
    /// <typeparam name="T">类型</typeparam>
    /// <returns>前代为Type的元素</returns>
    public static IEnumerable<T> FindVisualAncestors<T>(DependencyObject child) where T : DependencyObject => FindVisualAncestors(child, typeof(T)).Cast<T>();

    /// <summary>
    /// 获取element的指定类型的前代为Type的第一个元素
    /// </summary>
    /// <param name="child">子元素</param>
    /// <param name="type">要查找的类型</param>
    /// <returns>前代为指定类型的第一个元素</returns>
    public static object? FindVisualAncestor(DependencyObject child, Type type)
    {
        var temp = child;
        while (temp is not null)
        {
            if (temp.GetType() == type)
            {
                return temp;
            }

            (temp as FrameworkElement)?.ApplyTemplate();

            temp = VisualTreeHelper.GetParent(temp);
        }

        return null;
    }

    /// <summary>
    /// 获取 element 的指定类型的前代为 type 的元素
    /// </summary>
    /// <param name="child">子元素</param>
    /// <param name="type">要查找的类型</param>
    /// <returns>前代为指定类型的元素</returns>
    public static IEnumerable<DependencyObject> FindVisualAncestors(DependencyObject child, Type type)
    {
        var temp = child;

        while (temp is not null)
        {
            if (temp.GetType() == type)
            {
                yield return temp;
            }

            (temp as FrameworkElement)?.ApplyTemplate();

            temp = VisualTreeHelper.GetParent(temp);
        }
    }

    #endregion Parrent
}
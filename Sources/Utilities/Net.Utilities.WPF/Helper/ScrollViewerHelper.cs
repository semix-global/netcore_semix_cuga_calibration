using System.Windows.Controls;

namespace Net.Utilities.WPF.Helper;

public static class ScrollViewerHelper
{
    /// <summary>
    /// 滚动到ListView的最后一行
    /// </summary>
    /// <param name="listView">ListView</param>
    public static void ScrollToEnd(ListView listView)
    {
        var scrollViewer = DependencyObjectHelper.FindVisualDescendant<ScrollViewer>(listView);
        scrollViewer?.ScrollToEnd();
    }
}
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Net.Utilities.WPF.Helper;

public static class VisualHelper
{
    /// <summary>
    /// 获取装饰器图层
    /// </summary>
    /// <param name="visual">元素</param>
    /// <returns>装饰器图层</returns>
    public static AdornerLayer? GetAdornerLayer(Visual visual)
    {
        return visual switch
        {
            AdornerDecorator decorator => decorator.AdornerLayer,
            ScrollContentPresenter presenter => presenter.AdornerLayer,
            Window window => AdornerLayer.GetAdornerLayer(window.Content as Visual ?? visual),
            _ => AdornerLayer.GetAdornerLayer(visual)
        };
    }
}
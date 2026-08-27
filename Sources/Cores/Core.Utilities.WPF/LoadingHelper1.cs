using HandyControl.Controls;
using HandyControl.Data;
using HandyControl.Interactivity;
using Net.Utilities.WPF.Helpers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Window = HandyControl.Controls.Window;

namespace Net.Utilities.WPF.HandyControl.AttachedHelper;

public static class LoadingHelper1
{
    private const double MinimumArcThickness = 3.5;
    private const double MinimumDiameter = 50;

    #region 附加属性

    public static readonly DependencyProperty IsShowProperty = DependencyProperty.RegisterAttached("IsShow", typeof(bool), typeof(LoadingHelper), new PropertyMetadata(false, OnIsShowChanged));

    public static bool GetIsShow(DependencyObject obj) => (bool)obj.GetValue(IsShowProperty);

    public static void SetIsShow(DependencyObject obj, bool value) => obj.SetValue(IsShowProperty, value);

    public static readonly DependencyProperty IsShowTimeProperty = DependencyProperty.RegisterAttached("IsShowTime", typeof(bool), typeof(LoadingHelper), new PropertyMetadata(false));

    public static bool GetIsShowTime(DependencyObject obj) => (bool)obj.GetValue(IsShowTimeProperty);

    public static void SetIsShowTime(DependencyObject obj, bool value) => obj.SetValue(IsShowTimeProperty, value);

    #endregion 附加属性

    private static void OnIsShowChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is not bool isShow || d is not FrameworkElement parent) return;

        if (isShow)
        {
            parent.IsVisibleChanged += ParentOnIsVisibleChanged;
            parent.Unloaded += ParentOnUnloaded;
            if (parent.IsLoaded == false) parent.Loaded += ParentOnLoaded;
            else CreateLoading(parent);
        }
        else
        {
            parent.Loaded -= ParentOnLoaded;
            parent.Unloaded -= ParentOnUnloaded;
            parent.IsVisibleChanged -= ParentOnIsVisibleChanged;

            CreateLoading(parent, true);
        }
    }

    private static void ParentOnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is not bool isVisible || sender is not FrameworkElement parent) return;

        switch (isVisible)
        {
            case true when GetIsShow(parent) && parent.IsLoaded:
                CreateLoading(parent);
                break;

            case false:
                CreateLoading(parent, true);
                break;
        }
    }

    private static void ParentOnUnloaded(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element) CreateLoading(element, true);
    }

    private static void ParentOnLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element) CreateLoading(element);
    }

    private static void CreateLoading(FrameworkElement frameworkElement, bool isRemove = false)
    {
        var oldFrameworkElement = frameworkElement;
        if (isRemove == false && oldFrameworkElement.IsVisible == false) return;

        if (oldFrameworkElement is Window window) // window不能添加装饰层
        {
            if (window.Content is FrameworkElement temp) frameworkElement = temp;
            else return;
        }
        else frameworkElement = oldFrameworkElement;

        // 获取装饰层图层
        var layer = VisualHelper.GetAdornerLayer(frameworkElement);
        if (layer is null) return;

        // 获取装饰器
        var adorers = layer.GetAdorners(frameworkElement);
        if (isRemove)
        {
            foreach (var item in adorers ?? [])
            {
                if (item is not AdornerContainer container) continue;

                if (container.Tag is DispatcherTimer removeTimer) removeTimer.Stop();

                container.Child = null;
                layer.Remove(container);
            }

            return;
        }

        var isLoading = GetIsShow(oldFrameworkElement);
        if (isLoading == false) return;

        // 创建装饰器
        var adornerContainer = new AdornerContainer(frameworkElement /*在那个上面显示遮罩(layer一般和userControl和window挂钩)*/);

        var size = Math.Min(frameworkElement.ActualHeight, frameworkElement.ActualWidth);
        size = size <= MinimumDiameter ? size : size / 2d;

        var circleProgressBar = new CircleProgressBar
        {
            IsIndeterminate = true,
            ArcThickness = Math.Max(size / 25d, MinimumArcThickness),
            ShowText = false,
            Foreground = Application.Current.FindResource(ResourceToken.DarkDangerBrush) as SolidColorBrush,
            Width = size,
            Height = size
        };

        if (GetIsShowTime(oldFrameworkElement))
        {
            var startDateTime = DateTime.Now;
            var timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            timer.Tick += (s, e) => { circleProgressBar.Text = (DateTime.Now - startDateTime).ToString(@"mm\:ss\.f"); };
            timer.Start();
            adornerContainer.Tag = timer;
        }

        adornerContainer.Child = new Grid // 遮罩层
        {
            Background = Brushes.White,
            Opacity = 0.5,
            Children = // 0行0列默认在居中显示
            {
                circleProgressBar
            }
        };
        layer.Add(adornerContainer);
    }
}
using CommunityToolkit.Mvvm.Messaging;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helper.IOC.Providers;
using Net.Utilities.Helper.Object;
using Net.Utilities.WPF.Helper;
using Net.Utilities.WPF.MVVM.Events;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Navigation;

namespace Net.Utilities.WPF.MVVM.Services.Impl;

[IOCAppService(ServiceType = typeof(IWindowManagerService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed class WindowManagerServiceImpl(
    IViewLocatorService viewLocatorService,
    ISynchronizationContextProvider contextProvider,
    IMessenger messenger) : IWindowManagerService
{
    public bool? ShowDialog<TVm>(TVm? viewModel = null, IDictionary<string, object?>? settings = null, bool isSendFrontWindow = true) where TVm : ViewModelBase
    {
        var result = default(bool?);
        contextProvider.Send(() =>
        {
            var window = CreateWindow(true, settings, viewModel);
            WindowHelper.CreateMask(window);
            if (isSendFrontWindow) window.Loaded += (_, _) => messenger.Send(FrontWindowEventFactory.Instance());

            result = window.ShowDialog();
        });
        return result;
    }

    public void ShowWindow<TVm>(TVm? viewModel = null, IDictionary<string, object?>? settings = null) where TVm : ViewModelBase
    {
        contextProvider.Send(() =>
        {
            NavigationWindow? navWindow = null;

            var application = Application.Current;
            if (application is { MainWindow: not null })
            {
                navWindow = application.MainWindow as NavigationWindow;
            }

            if (navWindow is not null)
            {
                var window = CreatePage(settings, viewModel);
                navWindow.Navigate(window);
            }
            else
            {
                CreateWindow(false, settings, viewModel).Show();
            }
        });
    }

    public void ShowPopup<TVm>(TVm? viewModel = null, IDictionary<string, object?>? settings = null) where TVm : ViewModelBase
    {
        contextProvider.Send(() =>
        {
            var popup = CreatePopup(settings);
            var view = viewModel is null ? viewLocatorService.ViewModelType2View<TVm>() : viewLocatorService.ViewModelType2View(viewModel.GetType());

            popup.Child = view;

            if (viewModel is not null) MVVMLocatorProvider.Bind(popup, viewModel);
            if (popup.DataContext is not null) ObjectHelper.ApplyProperties(popup.DataContext, settings);

            popup.IsOpen = true;
            popup.CaptureMouse();
        });
    }

    /// <summary>
    /// 创建一个窗口
    /// </summary>
    /// <param name="isDialog">无论窗口是否显示为对话框</param>
    /// <param name="settings">可选的传参</param>
    /// <param name="viewModel">viewModel</param>
    /// <returns>窗口</returns>
    private Window CreateWindow<TVm>(bool isDialog, IDictionary<string, object?>? settings, TVm? viewModel = null) where TVm : ViewModelBase
    {
        var view = WindowHelper.EnsureWindow(viewModel is null ? viewLocatorService.ViewModelType2View<TVm>() : viewLocatorService.ViewModelType2View(viewModel.GetType()), isDialog);
        if (viewModel is not null) MVVMLocatorProvider.Bind(view, viewModel);
        if (view.DataContext is not null) ObjectHelper.ApplyProperties(view.DataContext, settings);

        ObjectHelper.ApplyProperties(view, settings);

        return view;
    }

    /// <summary>
    /// 创建一个Page
    /// </summary>
    /// <param name="settings">可选的传参</param>
    /// <param name="viewModel">viewModel</param>
    /// <returns>Page</returns>
    private Page CreatePage<TVm>(IDictionary<string, object?>? settings, TVm? viewModel = null) where TVm : ViewModelBase
    {
        var view = PageHelper.EnsurePage(viewModel is null ? viewLocatorService.ViewModelType2View<TVm>() : viewLocatorService.ViewModelType2View(viewModel.GetType()));
        if (viewModel is not null) MVVMLocatorProvider.Bind(view, viewModel);
        if (view.DataContext is not null) ObjectHelper.ApplyProperties(view.DataContext, settings);

        ObjectHelper.ApplyProperties(view, settings);

        return view;
    }

    /// <summary>
    /// 创建用于托管弹出窗口的弹出窗口
    /// </summary>
    /// <param name="settings">可选的传参</param>
    /// <returns>在当前鼠标位置的弹出窗口</returns>
    private static Popup CreatePopup(IDictionary<string, object?>? settings)
    {
        var popup = new Popup
        {
            StaysOpen = false,
            PopupAnimation = PopupAnimation.Slide
        };

        if (ObjectHelper.ApplyProperties(popup, settings))
        {
            if (settings?.ContainsKey("PlacementTarget") == true == false && settings?.ContainsKey("Placement") == true == false)
                popup.Placement = PlacementMode.MousePoint;

            if (settings?.ContainsKey("AllowsTransparency") == true == false)
                popup.AllowsTransparency = true;
        }
        else
        {
            popup.AllowsTransparency = true;
            popup.Placement = PlacementMode.MousePoint;
        }

        return popup;
    }
}
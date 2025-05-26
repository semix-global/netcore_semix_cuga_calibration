using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helper.Object;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Net.Utilities.WPF.MVVM.Services.Impl;

[IOCAppService(ServiceType = typeof(INavigationService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed class NavigationServiceImpl : INavigationService
{
    private readonly Frame _mainFrame;
    private readonly IViewLocatorService _viewLocatorService;

    public NavigationServiceImpl(Frame frame, IViewLocatorService viewLocatorService)
    {
        _mainFrame = frame;
        _viewLocatorService = viewLocatorService;

        _mainFrame.LoadCompleted += MainFrameOnLoadCompleted;
    }

    private void MainFrameOnLoadCompleted(object sender, NavigationEventArgs e)
    {
        if (e.ExtraData is not Dictionary<string, object?> settings) return;

        if ((_mainFrame.Content as FrameworkElement)?.DataContext is not ViewModelBase vm) return;

        ObjectHelper.ApplyProperties(vm, settings);
    }

    public bool CanGoBack => _mainFrame.CanGoBack;

    public void GoBack() => _mainFrame.GoBack();

    public bool CanGoForward => _mainFrame.CanGoForward;

    public void GoForward() => _mainFrame.GoForward();

    public IEnumerable BackStack => _mainFrame.BackStack;

    public void Refresh() => _mainFrame.Refresh();

    public bool Navigate<TVm>(IDictionary<string, object?>? settings = null) where TVm : ViewModelBase
    {
        var page = _viewLocatorService.ViewModelType2View<TVm>();
        return _mainFrame.Navigate(page, settings);
    }
}
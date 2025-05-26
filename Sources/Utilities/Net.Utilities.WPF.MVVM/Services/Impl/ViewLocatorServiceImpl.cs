using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.WPF.MVVM.Providers;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace Net.Utilities.WPF.MVVM.Services.Impl;

[IOCAppService(ServiceType = typeof(IViewLocatorService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed class ViewLocatorServiceImpl(IServiceProvider serviceProvider) : IViewLocatorService
{
    public UIElement ViewType2View(Type viewType)
    {
        if (serviceProvider.GetService(viewType) is UIElement view) return view;

        var viewTypeInfo = viewType.GetTypeInfo();
        var uiElementInfo = typeof(UIElement).GetTypeInfo();

        if (viewTypeInfo.IsInterface || viewTypeInfo.IsAbstract || uiElementInfo.IsAssignableFrom(viewTypeInfo) == false)
            return new TextBlock { Text = $"Cannot create {viewType.FullName}." };

        return (UIElement)Activator.CreateInstance(viewType)!;
    }

    public UIElement ViewModelType2View(Type? viewModelType)
    {
        var viewType = MVVMLocatorProvider.ViewModelType2ViewType(viewModelType);
        return viewType is null
            ? new TextBlock { Text = $"Cannot find view for {viewModelType}." }
            : ViewType2View(viewType);
    }

    public UIElement ViewModelType2View<TVm>() where TVm : ViewModelBase
    {
        return ViewModelType2View(typeof(TVm));
    }
}
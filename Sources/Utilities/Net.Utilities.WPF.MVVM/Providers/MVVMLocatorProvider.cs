using Net.Utilities.WPF.MVVM.AttachedHelper;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using System.Reflection;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace Net.Utilities.WPF.MVVM.Providers;

// ReSharper disable once InconsistentNaming
public static class MVVMLocatorProvider
{
    #region MVVM ViewModel And View

    /// <summary>
    /// ViewModel类型 to View类型
    /// </summary>
    /// <param name="viewModelType">ViewModel类型</param>
    /// <returns>View类型</returns>
    public static Type? ViewModelType2ViewType(Type? viewModelType)
    {
        if (viewModelType?.IsSubclassOf(typeof(ViewModelBase)) == true == false) return null;

        return Assembly.GetAssembly(viewModelType)
            ?.GetTypes()
            .FirstOrDefault(t => t.Name.StartsWith(viewModelType.Name.Replace("ViewModel", string.Empty)) && t.Name != viewModelType.Name);
    }

    /// <summary>
    /// View类型 to ViewModel类型
    /// </summary>
    /// <param name="viewType">View类型</param>
    /// <returns>ViewModel类型</returns>
    public static Type? ViewType2ViewModelType(Type? viewType)
    {
        if (viewType?.IsSubclassOf(typeof(FrameworkElement)) == true == false) return null;

        return Assembly.GetAssembly(viewType)
            ?.GetTypes()
            .FirstOrDefault(t => t.Name.StartsWith($"{viewType.Name.Replace("UserControl", string.Empty)}ViewModel") && t.Name != viewType.Name);
    }

    /// <summary>
    /// 将View与ViewModel数据上下文绑定
    /// </summary>
    /// <typeparam name="TVm">ViewModel类型</typeparam>
    /// <param name="frameworkElement">View</param>
    /// <param name="dataContext">ViewModel</param>
    public static void Bind<TVm>(FrameworkElement frameworkElement, TVm dataContext) where TVm : ViewModelBase
    {
        frameworkElement.DataContext = dataContext;
        if (dataContext is ViewModelBase viewModelBase && frameworkElement is Window or Popup) viewModelBase.Bind(frameworkElement);
    }

    #endregion MVVM ViewModel And View

    #region Attach Property

    /// <summary>
    /// 自动注入DataContext
    /// </summary>
    /// <param name="view">view</param>
    public static void AutoWireViewModel(object view)
    {
        if (view is FrameworkElement { DataContext: null } frameworkElement && AutoWireViewModelHelper.GetIsAutoWireViewModel(frameworkElement) is null)
        {
            AutoWireViewModelHelper.SetIsAutoWireViewModel(frameworkElement, true);
        }
    }

    /// <summary>
    /// 自动装配事件改变
    /// </summary>
    /// <param name="view">View</param>
    /// <exception cref="ArgumentException">参数异常</exception>
    internal static void AutoWireViewModelChanged(FrameworkElement view)
    {
        var viewModelType = ViewType2ViewModelType(view.GetType());
        if (viewModelType is null) throw new ArgumentException($"{nameof(viewModelType)} is null");

        var dataContext = HostApplication.GetRequiredService(viewModelType);
        if (dataContext is not ViewModelBase viewModel) throw new ArgumentException($"{nameof(dataContext)} is not {nameof(ViewModelBase)}");

        Bind(view, viewModel);
    }

    #endregion Attach Property
}
using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using System.Windows;

namespace Net.Utilities.WPF.MVVM.Services;

public interface IViewLocatorService
{
    /// <summary>
    /// 根据View类型从IoC容器中检索View
    /// </summary>
    /// <param name="viewType">View类型</param>
    /// <returns>View</returns>
    UIElement ViewType2View(Type viewType);

    /// <summary>
    /// 找到指定ViewModel类型的View
    /// </summary>
    /// <param name="viewModelType">ViewModel类型</param>
    /// <returns>View</returns>
    UIElement ViewModelType2View(Type? viewModelType);

    /// <summary>
    /// 找到指定ViewModel类型的View
    /// </summary>
    /// <typeparam name="TVm">ViewModel类型</typeparam>
    /// <returns>View</returns>
    UIElement ViewModelType2View<TVm>() where TVm : ViewModelBase;
}
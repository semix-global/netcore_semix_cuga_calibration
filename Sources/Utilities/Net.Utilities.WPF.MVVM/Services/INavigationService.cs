using Net.Utilities.WPF.MVVM.ViewModels.Bases;
using System.Collections;

namespace Net.Utilities.WPF.MVVM.Services;

public interface INavigationService
{
    /// <summary>
    /// 导航是否能够后退
    /// </summary>
    public bool CanGoBack { get; }

    /// <summary>
    /// 导航后退
    /// </summary>
    public void GoBack();

    /// <summary>
    /// 导航是否能够前进
    /// </summary>
    public bool CanGoForward { get; }

    /// <summary>
    /// 前进
    /// </summary>
    public void GoForward();

    /// <summary>
    /// 导航历史
    /// </summary>
    public IEnumerable BackStack { get; }

    /// <summary>
    /// 重新加载内容
    /// </summary>
    public void Refresh();

    /// <summary>
    /// 导航到指定的ViewModel
    /// </summary>
    /// <typeparam name="TVm">ViewModel类型</typeparam>
    /// <param name="settings">可选的传参</param>
    /// <returns>是否成功</returns>
    bool Navigate<TVm>(IDictionary<string, object?>? settings = null) where TVm : ViewModelBase;
}
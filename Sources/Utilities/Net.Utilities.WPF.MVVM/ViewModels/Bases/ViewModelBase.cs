using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace Net.Utilities.WPF.MVVM.ViewModels.Bases;

public class ViewModelBase : ObservableObject
{
    /// <summary>
    /// 可关闭的 View 缓存
    /// </summary>
    private readonly List<WeakReference> _views = [];

    /// <summary>
    /// 将View与ViewModel数据上下文绑定
    /// </summary>
    /// <param name="view">View</param>
    internal void Bind(object view)
    {
        _views.Add(new WeakReference(view));
    }

    /// <summary>
    /// 关闭所有View
    /// </summary>
    /// <param name="dialogResult">dialog Result</param>
    protected virtual void CloseView(bool? dialogResult)
    {
        var contextualViews = _views.Select(weakReference => weakReference.Target).ToList();

        _views.Clear();
        foreach (var contextualView in contextualViews)
        {
            HostApplication.ContextProvider.Send(() =>
            {
                switch (contextualView)
                {
                    case Window window when dialogResult is not null && window.Owner is not null:
                        window.DialogResult = dialogResult;
                        break;

                    case Window window:
                        window.Close();
                        break;

                    case Popup popup:
                        popup.IsOpen = false;
                        break;
                }
            });
        }

        GC.Collect();
    }

    /// <summary>
    /// 显示所有窗体
    /// </summary>
    /// <param name="isShowDialog">dialog显示</param>
    protected virtual bool ShowView(bool isShowDialog)
    {
        var contextualViews = _views.Select(weakReference => weakReference.Target).ToList();
        if (contextualViews.Count == 0 || contextualViews.Any(t => t is null)) return false;

        var result = true;
        Exception? ex = null;
        foreach (var contextualView in contextualViews)
        {
            if (result == false || ex is not null) break;

            HostApplication.ContextProvider.Send(() =>
            {
                try
                {
                    switch (contextualView)
                    {
                        case Window window:
                            if (PresentationSource.FromVisual(window) is null) // 对象的呈现源为空, Visual已经被移除或销毁
                            {
                                result = false;
                                break;
                            }

                            if (isShowDialog) window.ShowDialog();
                            else window.Show();
                            result = true;

                            break;

                        case Popup popup:
                            if (PresentationSource.FromVisual(popup) is null) // 对象的呈现源为空, Visual已经被移除或销毁
                            {
                                result = false;
                                break;
                            }

                            popup.IsOpen = true;
                            result = true;

                            break;
                    }
                }
                catch (Exception e)
                {
                    result = false;
                    ex = e;
                }
            });
        }

        if (ex is not null) throw ex;

        return result;
    }
}
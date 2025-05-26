using Net.Utilities.WPF.MVVM.ViewModels.Bases;

namespace Net.Utilities.WPF.MVVM.Services;

public interface IWindowManagerService
{
    /// <summary>
    /// 显示指定ViewModel的模态对话框
    /// </summary>
    /// <typeparam name="TVm">ViewModel类型</typeparam>
    /// <param name="viewModel">ViewModel</param>
    /// <param name="settings">可选的传参</param>
    /// <param name="isSendFrontWindow">是否发送置顶窗口</param>
    /// <returns>是否成功</returns>
    bool? ShowDialog<TVm>(TVm? viewModel = null, IDictionary<string, object?>? settings = null, bool isSendFrontWindow = true) where TVm : ViewModelBase;

    /// <summary>
    /// 显示指定ViewModel的非模态窗口
    /// </summary>
    /// <typeparam name="TVm">ViewModel类型</typeparam>
    /// <param name="viewModel">ViewModel</param>
    /// <param name="settings">可选的传参</param>
    void ShowWindow<TVm>(TVm? viewModel = null, IDictionary<string, object?>? settings = null) where TVm : ViewModelBase;

    /// <summary>
    /// 显示指定ViewModel在当前鼠标位置的弹出窗口
    /// </summary>
    /// <typeparam name="TVm">ViewModel类型</typeparam>
    /// <param name="viewModel">ViewModel</param>
    /// <param name="settings">可选的传参</param>
    void ShowPopup<TVm>(TVm? viewModel = null, IDictionary<string, object?>? settings = null) where TVm : ViewModelBase;
}
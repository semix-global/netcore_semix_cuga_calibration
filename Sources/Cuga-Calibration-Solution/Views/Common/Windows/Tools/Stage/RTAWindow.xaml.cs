using CugaCalibration.ViewModels.Common.Windows.Tools.Stage;
using System.ComponentModel;

namespace CugaCalibration.Views.Common.Windows.Tools.Stage;

public partial class RTAWindow
{
    public RTAWindow()
    {
        InitializeComponent();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);

        if (DataContext is not RTAWindowViewModel viewModel || viewModel.IsCloseReady) return;

        e.Cancel = true;

        if (viewModel.IsClosing) return;

        // 系统关闭事件先退出当前调用栈, 再取消任务并等待缓存保存.
        Dispatcher.BeginInvoke(new Action(() =>
        {
            if (viewModel.CloseCommand.CanExecute(null)) viewModel.CloseCommand.Execute(null);
        }));
    }
}
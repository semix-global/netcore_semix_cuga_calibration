using CommunityToolkit.Mvvm.Messaging;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.WPF.Helper;
using Net.Utilities.WPF.MVVM.Events;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;

namespace CugaCalibration.Views.Common.Windows.View;

[IOCAppService(ServiceType = typeof(StageWindow), IOCLifetimeEnum = IOCLifeTimeEnum.Transient)]
public sealed partial class StageWindow : IRecipient<FrontWindowEvent>
{
    private readonly IMessenger _messenger;
    private bool _isLoaded;

    public StageWindow(IMessenger messenger)
    {
        InitializeComponent();

        _messenger = messenger;
        messenger.RegisterAll(this);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_isLoaded) return;
        _isLoaded = true;

        WindowStartupLocation = WindowStartupLocation.Manual;

        var primaryScreen = Screen.PrimaryScreen;
        // 设置弹窗的位置在主窗体的右下角
        Left = primaryScreen.WorkingArea.Right - RenderSize.Width - 10;
        Top = primaryScreen.WorkingArea.Bottom - RenderSize.Height - 10;
    }

    private void OnClosed(object sender, EventArgs e)
    {
        _messenger.UnregisterAll(this);
    }

    public void Receive(FrontWindowEvent message)
    {
        var handle = new WindowInteropHelper(this).Handle;
        User32WrapperHelper.EnableWindow(handle, true);
    }
}
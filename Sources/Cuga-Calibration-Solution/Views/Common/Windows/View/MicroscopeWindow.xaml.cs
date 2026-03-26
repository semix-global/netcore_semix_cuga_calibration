using CommunityToolkit.Mvvm.Messaging;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.WPF.Helper;
using Net.Utilities.WPF.MVVM.Events;
using System.Windows;
using System.Windows.Interop;

namespace CugaCalibration.Views.Common.Windows.View;

[IOCAppService(ServiceType = typeof(MicroscopeWindow), IOCLifetimeEnum = IOCLifeTimeEnum.Transient)]
public sealed partial class MicroscopeWindow : IRecipient<FrontWindowEvent>
{
    private readonly IMessenger _messenger;
    private bool _isLoaded;

    public MicroscopeWindow(IMessenger messenger)
    {
        InitializeComponent();

        _messenger = messenger;
        messenger.RegisterAll(this);
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (_isLoaded) return;
        _isLoaded = true;

        WindowStartupLocation = WindowStartupLocation.Manual;

        // 设置弹窗的位置在主窗体的左下角
        Left = SystemParameters.WorkArea.Left + 10;
        Top = SystemParameters.WorkArea.Bottom - RenderSize.Height - 10;
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _messenger.UnregisterAll(this);
    }

    public void Receive(FrontWindowEvent message)
    {
        var handle = new WindowInteropHelper(this).Handle;
        User32WrapperHelper.EnableWindow(handle, true);
    }
}
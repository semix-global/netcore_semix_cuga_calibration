using CommunityToolkit.Mvvm.Messaging;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.WPF.Helper;
using Net.Utilities.WPF.MVVM.Events;
using System.Windows.Interop;

namespace CugaCalibration.Views.Common.Windows.View;

[IOCAppService(ServiceType = typeof(CalibrationMenuWindow), IOCLifetimeEnum = IOCLifeTimeEnum.Transient)]
public sealed partial class CalibrationMenuWindow : IRecipient<FrontWindowEvent>
{
    private readonly IMessenger _messenger;

    public CalibrationMenuWindow(IMessenger messenger)
    {
        InitializeComponent();

        _messenger = messenger;
        messenger.RegisterAll(this);
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
using Core.Utilities.WPF.Tray.Native;
using System.Windows.Interop;

namespace Core.Utilities.WPF.Tray.Service.Implements;

internal sealed class TrayMessageWindow : IDisposable
{
    private readonly HwndSource _hwndSource;
    private readonly uint _taskbarCreatedMessageId;
    private bool _disposed;

    public event EventHandler? MouseRightButtonUp;
    public event EventHandler? TaskbarCreated;

    public IntPtr Handle => _hwndSource.Handle;

    public TrayMessageWindow()
    {
        _taskbarCreatedMessageId = User32.RegisterWindowMessage("TaskbarCreated");

        var parameters = new HwndSourceParameters("TrayMessageWindow")
        {
            ParentWindow = User32.HwndMessage,
            WindowStyle = 0,
            ExtendedWindowStyle = 0
        };

        _hwndSource = new HwndSource(parameters);
        _hwndSource.AddHook(WndProc);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == Shell32.WmTrayCallbackMessage)
        {
            var mouseMessage = (uint)lParam.ToInt64();
            if (mouseMessage == Shell32.WmRbuttonup)
            {
                MouseRightButtonUp?.Invoke(this, EventArgs.Empty);
            }

            handled = true;
            return IntPtr.Zero;
        }

        if (msg == _taskbarCreatedMessageId)
        {
            TaskbarCreated?.Invoke(this, EventArgs.Empty);
            handled = true;
        }

        return IntPtr.Zero;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _hwndSource.RemoveHook(WndProc);
        _hwndSource.Dispose();
    }
}
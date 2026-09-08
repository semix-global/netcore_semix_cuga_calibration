using Core.Utilities.WPF.Tray.Model;
using Core.Utilities.WPF.Tray.Native;
using Core.Utilities.WPF.Tray.Service.Interfaces;
using Core.Utilities.WPF.Tray.UI;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Core.Utilities.WPF.Tray.Service.Implements;

public sealed class TrayService : ITrayService, IDisposable
{
    private TrayMessageWindow? _messageWindow;
    private Notifyicondata _nid;
    private Icon? _icon;
    private ContextMenu? _contextMenu;
    private bool _isAdded;
    private bool _disposed;

    public void Initialize(TrayOptions options)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(TrayService));
        }

        DisposeCore();

#pragma warning disable IDISP003
        _messageWindow = new TrayMessageWindow();
        _messageWindow.MouseRightButtonUp += OnMouseRightButtonUp;
        _messageWindow.TaskbarCreated += OnTaskbarCreated;

        _icon = Converters.IconConverter.ToIcon(options.Icon);
#pragma warning restore IDISP003

        _nid = new Notifyicondata
        {
            cbSize = (uint)Marshal.SizeOf<Notifyicondata>(),
            hWnd = _messageWindow.Handle,
            uID = 1,
            uFlags = Shell32.NifMessage | Shell32.NifIcon | Shell32.NifTip,
            uCallbackMessage = Shell32.WmTrayCallbackMessage,
            hIcon = _icon?.Handle ?? IntPtr.Zero,
            szTip = TruncateTip(options.ToolTip)
        };

        if (!Shell32.Shell_NotifyIcon(Shell32.NimAdd, ref _nid))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        _isAdded = true;

        _contextMenu = options.ContextMenu ?? new TrayMenu();
        if (_contextMenu.DataContext == null)
        {
            _contextMenu.DataContext = options.DataContext;
        }
    }

    private void OnMouseRightButtonUp(object? sender, EventArgs e)
    {
        var dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
        _ = dispatcher.BeginInvoke(ShowContextMenu);
    }

    private void ShowContextMenu()
    {
        if (_contextMenu == null || _messageWindow == null)
        {
            return;
        }

        _contextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
        _contextMenu.PlacementTarget = null;

        // Required so the menu closes when focus is lost.
        User32.SetForegroundWindow(_messageWindow.Handle);

        _contextMenu.IsOpen = true;
    }

    private static string TruncateTip(string? tip)
    {
        const int maxLength = 127;

        if (string.IsNullOrEmpty(tip))
        {
            return string.Empty;
        }

        return tip.Length > maxLength ? tip[..maxLength] : tip;
    }

    private void OnTaskbarCreated(object? sender, EventArgs e)
    {
        if (_isAdded)
        {
            Shell32.Shell_NotifyIcon(Shell32.NimAdd, ref _nid);
        }
    }

    public void Dispose()
    {
        DisposeCore();
        _disposed = true;
    }

    private void DisposeCore()
    {
        if (_messageWindow != null)
        {
            _messageWindow.MouseRightButtonUp -= OnMouseRightButtonUp;
            _messageWindow.TaskbarCreated -= OnTaskbarCreated;
            _messageWindow.Dispose();
            _messageWindow = null;
        }

        if (_isAdded)
        {
            var nid = _nid;
            Shell32.Shell_NotifyIcon(Shell32.NimDelete, ref nid);
            _isAdded = false;
        }

        _icon?.Dispose();
        _icon = null;
    }
}
using System.Runtime.InteropServices;

namespace Core.Utilities.WPF.Tray.Native;

internal static class Shell32
{
    public const uint NimAdd = 0x00000000;
    public const uint NimModify = 0x00000001;
    public const uint NimDelete = 0x00000002;
    public const uint NimSetversion = 0x00000004;

    public const uint NifMessage = 0x00000001;
    public const uint NifIcon = 0x00000002;
    public const uint NifTip = 0x00000004;

    public const uint NotifyiconVersion4 = 4;

    public const uint WmUser = 0x0400;
    public const uint WmTrayCallbackMessage = WmUser + 1;

    public const uint WmLbuttondown = 0x0201;
    public const uint WmLbuttonup = 0x0202;
    public const uint WmLbuttondblclk = 0x0203;
    public const uint WmRbuttondown = 0x0204;
    public const uint WmRbuttonup = 0x0205;
    public const uint WmRbuttondblclk = 0x0206;

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    public static extern bool Shell_NotifyIcon(uint dwMessage, ref Notifyicondata lpData);
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal struct Notifyicondata
{
    public uint cbSize;
    public IntPtr hWnd;
    public uint uID;
    public uint uFlags;
    public uint uCallbackMessage;
    public IntPtr hIcon;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
    public string szTip;
    public uint dwState;
    public uint dwStateMask;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
    public string szInfo;
    public uint uVersion;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
    public string szInfoTitle;
    public uint dwInfoFlags;
    public Guid guidItem;
    public IntPtr hBalloonIcon;
}

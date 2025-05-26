using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;

namespace Net.Utilities.WPF.UI.Controls;

public sealed class PopupNonTopmost : Popup
{
    public static readonly DependencyProperty TopmostProperty = Window.TopmostProperty.AddOwner(
        typeof(PopupNonTopmost),
        new FrameworkPropertyMetadata(false, OnTopmostChanged));

    public bool Topmost
    {
        get => (bool)GetValue(TopmostProperty);
        set => SetValue(TopmostProperty, value);
    }

    private static void OnTopmostChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
        if (sender is not PopupNonTopmost popupNonTopmost) return;

        popupNonTopmost.UpdateWindow();
    }

    protected override void OnOpened(EventArgs e)
    {
        UpdateWindow();
    }

    private void UpdateWindow()
    {
        var fromVisual = (HwndSource?)PresentationSource.FromVisual(Child); // 获取当前Popup的句柄
        if (fromVisual is null) return;

        var hWnd = fromVisual.Handle;
        if (GetWindowRect(hWnd, out var rect))
        {
            _ = SetWindowPos(hWnd, Topmost ? -1 : -2, rect.Left, rect.Top, Convert.ToInt32(Width), Convert.ToInt32(Height), 0);
        }
    }

    #region P/Invoke imports & definitions

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

#pragma warning disable IDE0079
#pragma warning disable SYSLIB1054 // 使用 “LibraryImportAttribute” 而不是 “DllImportAttribute” 在编译时生成 P/Invoke 封送代码

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(nint hWnd, out Rect lpRect); // nint 对应IntPtr

    [DllImport("user32.dll")]
    private static extern int SetWindowPos(nint hWnd, int hWndInsertAfter, int x, int y, int cx, int cy, int wFlags);

#pragma warning restore SYSLIB1054 // 使用 “LibraryImportAttribute” 而不是 “DllImportAttribute” 在编译时生成 P/Invoke 封送代码
#pragma warning restore IDE0079

    #endregion P/Invoke imports & definitions
}
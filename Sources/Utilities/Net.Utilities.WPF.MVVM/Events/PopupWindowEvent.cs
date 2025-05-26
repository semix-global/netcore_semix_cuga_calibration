namespace Net.Utilities.WPF.MVVM.Events;

public sealed class PopupWindowEvent
{
    public bool IsPopupWindowEnable { get; init; }
}

public static class PopupWindowEventFactory
{
    public static PopupWindowEvent EnableIsPopupWindowEnable() => new() { IsPopupWindowEnable = true };

    public static PopupWindowEvent DisableIsPopupWindowEnable() => new() { IsPopupWindowEnable = false };
}
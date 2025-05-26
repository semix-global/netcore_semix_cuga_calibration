namespace Net.Utilities.WPF.MVVM.Events;

public sealed class FrontWindowEvent;

public static class FrontWindowEventFactory
{
    public static FrontWindowEvent Instance() => new();
}
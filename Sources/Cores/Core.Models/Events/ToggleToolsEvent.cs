using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Core.Models.Events;

public sealed class ToggleToolsEvent
{
    public bool? IsToolsWindowEnable { get; set; } = false;
}

public static class ToggleToolsEventFactory
{
    public static ValueChangedMessage<ToggleToolsEvent> RefreshToolsWindowEnableStatus(bool value)
    {
        return new ValueChangedMessage<ToggleToolsEvent>(new ToggleToolsEvent
        {
            IsToolsWindowEnable = value
        });
    }
}
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Core.Models.Events;

public sealed class ToggleAutoCalibrateEvent
{
    public bool? IsAutoCalibrateEnable { get; set; } = false;
}

public static class ToggleAutoCalibrateEventFactory
{
    public static ValueChangedMessage<ToggleAutoCalibrateEvent> RefreshAutoCalibrateStatus(bool? value)
    {
        return new ValueChangedMessage<ToggleAutoCalibrateEvent>(new ToggleAutoCalibrateEvent
        {
            IsAutoCalibrateEnable = value
        });
    }
}
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Core.Models.Events;

public sealed class ToggleWaferMapEvent
{
    public bool? IsRefreshWaferMap { get; set; } = false;
}

public static class ToggleWaferMapEventFactory
{
    public static ValueChangedMessage<ToggleWaferMapEvent> UpdateIsRefreshWaferMap(bool? value)
    {
        return new ValueChangedMessage<ToggleWaferMapEvent>(new ToggleWaferMapEvent
        {
            IsRefreshWaferMap = value
        });
    }
}
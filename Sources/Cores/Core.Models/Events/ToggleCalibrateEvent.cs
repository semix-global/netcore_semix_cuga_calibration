using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Core.Models.Events;

public sealed class ToggleCalibrateEvent
{
    public bool? IsCalibrateEnable { get; set; } = false;

    public bool? IsReviewEnable { get; set; } = false;

    public bool? IsCancelEnable { get; set; } = false;

    public bool? IsPreviousEnable { get; set; } = false;

    public bool? IsNextEnable { get; set; } = false;
}

public static class ToggleCalibrateEventFactory
{
    public static ValueChangedMessage<ToggleCalibrateEvent> UpdateIsCalibrateEnable(bool value)
    {
        return new ValueChangedMessage<ToggleCalibrateEvent>(new ToggleCalibrateEvent
        {
            IsCalibrateEnable = value,
            IsReviewEnable = null,
            IsCancelEnable = null,
            IsPreviousEnable = null,
            IsNextEnable = null
        });
    }

    public static ValueChangedMessage<ToggleCalibrateEvent> UpdateIsReviewEnable(bool value)
    {
        return new ValueChangedMessage<ToggleCalibrateEvent>(new ToggleCalibrateEvent
        {
            IsCalibrateEnable = null,
            IsReviewEnable = value,
            IsCancelEnable = null,
            IsPreviousEnable = null,
            IsNextEnable = null
        });
    }

    public static ValueChangedMessage<ToggleCalibrateEvent> UpdateIsCancelEnable(bool value)
    {
        return new ValueChangedMessage<ToggleCalibrateEvent>(new ToggleCalibrateEvent
        {
            IsCalibrateEnable = null,
            IsReviewEnable = null,
            IsCancelEnable = value,
            IsPreviousEnable = null,
            IsNextEnable = null
        });
    }

    public static ValueChangedMessage<ToggleCalibrateEvent> UpdateIsPreviousEnable(bool value)
    {
        return new ValueChangedMessage<ToggleCalibrateEvent>(new ToggleCalibrateEvent
        {
            IsCalibrateEnable = null,
            IsReviewEnable = null,
            IsCancelEnable = null,
            IsPreviousEnable = value,
            IsNextEnable = null
        });
    }

    public static ValueChangedMessage<ToggleCalibrateEvent> UpdateIsNextEnable(bool value)
    {
        return new ValueChangedMessage<ToggleCalibrateEvent>(new ToggleCalibrateEvent
        {
            IsCalibrateEnable = null,
            IsReviewEnable = null,
            IsCancelEnable = null,
            IsPreviousEnable = null,
            IsNextEnable = value
        });
    }

    public static ValueChangedMessage<ToggleCalibrateEvent> UpdateAll(bool? value)
    {
        return new ValueChangedMessage<ToggleCalibrateEvent>(new ToggleCalibrateEvent
        {
            IsCalibrateEnable = value,
            IsReviewEnable = value,
            IsCancelEnable = value,
            IsPreviousEnable = value,
            IsNextEnable = value
        });
    }

    public static ValueChangedMessage<ToggleCalibrateEvent> Enable()
    {
        return UpdateAll(true);
    }

    public static ValueChangedMessage<ToggleCalibrateEvent> Disable()
    {
        return UpdateAll(false);
    }
}
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Core.Models.Events;

public sealed class ToggleCalibrateEvent
{
    public bool? IsCalibrateEnable { get; set; } = false;

    public bool? IsReviewEnable { get; set; } = false;

    public bool? IsCancelEnable { get; set; } = false;

    public bool? IsPreviousEnable { get; set; } = false;

    public bool? IsNextEnable { get; set; } = false;

    public bool? IsRefreshMenuStatus { get; set; } = false;

    public bool? IsWindowEnable { get; set; } = false;


    public ToggleCalibrateEvent UpdateIsCalibrateEnable(bool value)
    {
        IsCalibrateEnable = value;
        IsReviewEnable = null;
        IsCancelEnable = null;
        IsPreviousEnable = null;
        IsNextEnable = null;
        return this;
    }

    public ToggleCalibrateEvent UpdateIsReviewEnable(bool value)
    {
        IsCalibrateEnable = null;
        IsReviewEnable = value;
        IsCancelEnable = null;
        IsPreviousEnable = null;
        IsNextEnable = null;
        return this;
    }

    public ToggleCalibrateEvent UpdateIsCancelEnable(bool value)
    {
        IsCalibrateEnable = null;
        IsReviewEnable = null;
        IsCancelEnable = value;
        IsPreviousEnable = null;
        IsNextEnable = null;
        return this;
    }

    public ToggleCalibrateEvent UpdateIsPreviousEnable(bool value)
    {
        IsCalibrateEnable = null;
        IsReviewEnable = null;
        IsCancelEnable = null;
        IsPreviousEnable = value;
        IsNextEnable = null;
        return this;
    }

    public ToggleCalibrateEvent UpdateIsNextEnable(bool value)
    {
        IsCalibrateEnable = null;
        IsReviewEnable = null;
        IsCancelEnable = null;
        IsPreviousEnable = null;
        IsNextEnable = value;
        return this;
    }

    public ToggleCalibrateEvent UpdateAll(bool? value)
    {
        IsCalibrateEnable = value;
        IsReviewEnable = value;
        IsCancelEnable = value;
        IsPreviousEnable = value;
        IsNextEnable = value;
        return this;
    }

    public ToggleCalibrateEvent Enable()
    {
        return UpdateAll(true);
    }

    public ToggleCalibrateEvent Disable()
    {
        return UpdateAll(false);
    }
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

    public static ValueChangedMessage<ToggleCalibrateEvent> UpdateWindowEnable(bool? value)
    {
        return new ValueChangedMessage<ToggleCalibrateEvent>(new ToggleCalibrateEvent
        {
            IsWindowEnable = value
        });
    }

    public static ValueChangedMessage<ToggleCalibrateEvent> RefreshMenuStatus(bool? value)
    {
        return new ValueChangedMessage<ToggleCalibrateEvent>(new ToggleCalibrateEvent
        {
            IsRefreshMenuStatus = value
        });
    }

    public static ValueChangedMessage<ToggleCalibrateEvent> ToggleCalibrateEvent(ToggleCalibrateEvent toggleCalibrateEvent)
    {
        return new ValueChangedMessage<ToggleCalibrateEvent>(toggleCalibrateEvent);
    }
}
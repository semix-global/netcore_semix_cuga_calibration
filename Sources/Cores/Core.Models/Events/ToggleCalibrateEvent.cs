using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Core.Models.Events;

public sealed class ToggleCalibrateEvent
{
    public bool? IsCalibrateEnable { get; set; } = false;

    public bool? IsReviewEnable { get; set; } = false;

    public bool? IsCancelEnable { get; set; } = false;

    public bool? IsPreviousEnable { get; set; } = false;

    public bool? IsNextEnable { get; set; } = false;

    public bool? IsRefreshWindow { get; set; } = false;

    public bool? IsWindowEnable { get; set; } = false;

    public ToggleCalibrateEvent UpdateIsCalibrateEnable(bool value)
    {
        IsCalibrateEnable = value;
        IsReviewEnable = null;
        IsCancelEnable = null;
        IsPreviousEnable = null;
        IsNextEnable = null;
        IsRefreshWindow = null;
        IsWindowEnable = null;
        return this;
    }

    public ToggleCalibrateEvent UpdateIsReviewEnable(bool value)
    {
        IsCalibrateEnable = null;
        IsReviewEnable = value;
        IsCancelEnable = null;
        IsPreviousEnable = null;
        IsNextEnable = null;
        IsRefreshWindow = null;
        IsWindowEnable = null;
        return this;
    }

    public ToggleCalibrateEvent UpdateIsCancelEnable(bool value)
    {
        IsCalibrateEnable = null;
        IsReviewEnable = null;
        IsCancelEnable = value;
        IsPreviousEnable = null;
        IsNextEnable = null;
        IsRefreshWindow = null;
        IsWindowEnable = null;
        return this;
    }

    public ToggleCalibrateEvent UpdateIsPreviousEnable(bool value)
    {
        IsCalibrateEnable = null;
        IsReviewEnable = null;
        IsCancelEnable = null;
        IsPreviousEnable = value;
        IsNextEnable = null;
        IsRefreshWindow = null;
        IsWindowEnable = null;
        return this;
    }

    public ToggleCalibrateEvent UpdateIsNextEnable(bool value)
    {
        IsCalibrateEnable = null;
        IsReviewEnable = null;
        IsCancelEnable = null;
        IsPreviousEnable = null;
        IsNextEnable = value;
        IsRefreshWindow = null;
        IsWindowEnable = null;
        return this;
    }

    public ToggleCalibrateEvent UpdateAll(bool? value)
    {
        IsCalibrateEnable = value;
        IsReviewEnable = value;
        IsCancelEnable = value;
        IsPreviousEnable = value;
        IsNextEnable = value;
        IsRefreshWindow = null;
        IsWindowEnable = null;
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
            IsNextEnable = null,
            IsRefreshWindow = null,
            IsWindowEnable = null
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
            IsNextEnable = null,
            IsRefreshWindow = null,
            IsWindowEnable = null
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
            IsNextEnable = null,
            IsRefreshWindow = null,
            IsWindowEnable = null
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
            IsNextEnable = null,
            IsRefreshWindow = null,
            IsWindowEnable = null
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
            IsNextEnable = value,
            IsRefreshWindow = null,
            IsWindowEnable = null
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
            IsNextEnable = value,
            IsRefreshWindow = null,
            IsWindowEnable = null
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
            IsWindowEnable = value,
            IsCalibrateEnable = null,
            IsRefreshWindow = null,
            IsReviewEnable = null,
            IsCancelEnable = null,
            IsPreviousEnable = null,
            IsNextEnable = null
        });
    }

    public static ValueChangedMessage<ToggleCalibrateEvent> RefreshWindow(bool? value)
    {
        return new ValueChangedMessage<ToggleCalibrateEvent>(new ToggleCalibrateEvent
        {
            IsRefreshWindow = value,
            IsWindowEnable = null,
            IsCalibrateEnable = null,
            IsReviewEnable = null,
            IsCancelEnable = null,
            IsPreviousEnable = null,
            IsNextEnable = null
        });
    }
}
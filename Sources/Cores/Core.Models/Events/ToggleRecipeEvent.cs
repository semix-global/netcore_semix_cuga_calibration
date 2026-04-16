using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Core.Models.Events;

public sealed class ToggleRecipeEvent
{
    public bool? IsRefreshRecipeList { get; set; } = false;

    public bool? IsEnableWaferMapEdit { get; set; } = false;

    public bool? IsRecipeAlignment { get; set; } = false;
}

public static class ToggleRecipeEventFactory
{
    public static ValueChangedMessage<ToggleRecipeEvent> RefreshRecipeManagementView(bool? value)
    {
        return new ValueChangedMessage<ToggleRecipeEvent>(new ToggleRecipeEvent
        {
            IsRefreshRecipeList = value,
            IsEnableWaferMapEdit = null,
            IsRecipeAlignment = null
        });
    }

    public static ValueChangedMessage<ToggleRecipeEvent> UpdateIsWaferMapEditEnable(bool? value)
    {
        return new ValueChangedMessage<ToggleRecipeEvent>(new ToggleRecipeEvent
        {
            IsEnableWaferMapEdit = value,
            IsRefreshRecipeList = null,
            IsRecipeAlignment = null
        });
    }

    public static ValueChangedMessage<ToggleRecipeEvent> UpdateIsRecipeAlignment(bool? value)
    {
        return new ValueChangedMessage<ToggleRecipeEvent>(new ToggleRecipeEvent
        {
            IsRecipeAlignment = value,
            IsRefreshRecipeList = null,
            IsEnableWaferMapEdit = null
        });
    }
}